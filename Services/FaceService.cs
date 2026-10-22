using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FaceAiSharp;
using FaceAiSharp.Extensions;
using NguyenHoaiPhong_1681.Data;
using NguyenHoaiPhong_1681.Entities;
using NguyenHoaiPhong_1681.Middleware;
using NguyenHoaiPhong_1681.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace NguyenHoaiPhong_1681.Services;

public record FaceMatchResult(double Score, bool Matched, bool Retry);

public interface IFaceService
{
    /// <summary>Tải trước model (gọi lúc khởi động để request đầu tiên không bị chậm).</summary>
    void WarmUp();

    /// <summary>Ảnh → embedding 512 chiều đã L2 normalize. Lỗi ảnh/khuôn mặt → AppException 400.</summary>
    Task<float[]> GetEmbeddingAsync(Stream image, bool requireOpenEyes = false);

    double CosineSimilarity(float[] a, float[] b);

    /// <summary>Đánh giá điểm theo ngưỡng + vùng xám (retry).</summary>
    FaceMatchResult Evaluate(double score);

    /// <summary>Đăng ký khuôn mặt từ nhiều ảnh: trung bình embedding rồi normalize, ghi đè bản cũ. Không lưu ảnh.</summary>
    Task<int> EnrollAsync(int userId, IReadOnlyList<Stream> images);

    /// <summary>So khớp 1:1 ảnh với vector đã đăng ký của user (không xét liveness — nơi gọi tự xử lý).</summary>
    Task<FaceMatchResult> VerifyUserAsync(int userId, Stream image);
}

public sealed class FaceService : IFaceService, IDisposable
{
    public const int MaxImageBytes = 2 * 1024 * 1024;
    private const int EmbeddingSize = 512;
    private const int MaxLongSide = 1600;
    private const double SampleConsistencyMin = 0.35;

    private sealed record Models(IFaceDetectorWithLandmarks Detector, IFaceEmbeddingsGenerator Embedder, IEyeStateDetector Eyes);

    private readonly Lazy<Models> _models;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly IServiceScopeFactory _scopes;
    private readonly FaceOptions _opt;
    private readonly ILogger<FaceService> _logger;

    public FaceService(IServiceScopeFactory scopes, IOptions<FaceOptions> opt, ILogger<FaceService> logger)
    {
        _scopes = scopes;
        _opt = opt.Value;
        _logger = logger;
        // Lazy<T> mặc định ExecutionAndPublication: model chỉ được nạp đúng 1 lần dù nhiều luồng gọi cùng lúc.
        _models = new Lazy<Models>(() =>
        {
            _logger.LogInformation("Đang nạp model khuôn mặt (SCRFD, ArcFace, eye-state)...");
            var m = new Models(
                FaceAiSharpBundleFactory.CreateFaceDetectorWithLandmarks(),
                FaceAiSharpBundleFactory.CreateFaceEmbeddingsGenerator(),
                FaceAiSharpBundleFactory.CreateEyeStateDetector());
            _logger.LogInformation("Đã nạp xong model khuôn mặt.");
            return m;
        });
    }

    public void WarmUp() => _ = _models.Value;

    public async Task<float[]> GetEmbeddingAsync(Stream image, bool requireOpenEyes = false)
    {
        var bytes = await ReadLimitedAsync(image);

        // Suy luận tuần tự: giữ bộ nhớ/CPU ổn định và tránh phụ thuộc vào tính thread-safe của từng thành phần.
        await _gate.WaitAsync();
        try
        {
            return await Task.Run(() => Compute(bytes, requireOpenEyes));
        }
        finally
        {
            _gate.Release();
        }
    }

    public double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0) return 0;
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        return na == 0 || nb == 0 ? 0 : dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    public FaceMatchResult Evaluate(double score)
    {
        var matched = score >= _opt.MatchThreshold;
        var retry = !matched && score >= _opt.MatchThreshold - _opt.GrayZone;
        return new FaceMatchResult(Math.Round(score, 4), matched, retry);
    }

    public async Task<int> EnrollAsync(int userId, IReadOnlyList<Stream> images)
    {
        var vectors = new List<float[]>();
        for (var i = 0; i < images.Count; i++)
        {
            try
            {
                vectors.Add(await GetEmbeddingAsync(images[i], requireOpenEyes: true));
            }
            catch (AppException ex)
            {
                throw new AppException(400, $"Ảnh {i + 1}: {ex.Message}");
            }
        }

        var mean = new float[EmbeddingSize];
        foreach (var v in vectors)
            for (var i = 0; i < EmbeddingSize; i++) mean[i] += v[i];
        Normalize(mean);

        // Các ảnh phải là cùng một người.
        for (var i = 0; i < vectors.Count; i++)
            if (CosineSimilarity(vectors[i], mean) < SampleConsistencyMin)
                throw new AppException(400, $"Ảnh {i + 1} không giống các ảnh còn lại, vui lòng chụp lại cùng một người");

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await db.FaceEmbeddings.FirstOrDefaultAsync(f => f.UserId == userId);
        if (entity == null)
        {
            entity = new FaceEmbedding { UserId = userId };
            db.FaceEmbeddings.Add(entity);
        }
        entity.Vector = mean;
        entity.SampleCount = vectors.Count;
        await db.SaveChangesAsync();
        return vectors.Count;
    }

    public async Task<FaceMatchResult> VerifyUserAsync(int userId, Stream image)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.FaceEmbeddings.AsNoTracking().Where(f => f.UserId == userId)
            .Select(f => f.Vector).FirstOrDefaultAsync()
            ?? throw new AppException(400, "Bạn chưa đăng ký khuôn mặt");

        var probe = await GetEmbeddingAsync(image);
        return Evaluate(CosineSimilarity(probe, stored));
    }

    private static async Task<byte[]> ReadLimitedAsync(Stream stream)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[16 * 1024];
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            ms.Write(buffer, 0, read);
            if (ms.Length > MaxImageBytes) throw new AppException(400, "Ảnh quá lớn (tối đa 2MB)");
        }
        if (ms.Length == 0) throw new AppException(400, "Ảnh không hợp lệ");
        return ms.ToArray();
    }

    private float[] Compute(byte[] bytes, bool requireOpenEyes)
    {
        var m = _models.Value;

        Image<Rgb24> img;
        try
        {
            var format = Image.DetectFormat(bytes);
            if (format is not (JpegFormat or PngFormat))
                throw new AppException(400, "Ảnh phải là JPEG hoặc PNG");
            img = Image.Load<Rgb24>(bytes);
        }
        catch (AppException) { throw; }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException or ArgumentException)
        {
            throw new AppException(400, "Ảnh không hợp lệ");
        }

        using (img)
        {
            img.Mutate(c => c.AutoOrient());
            if (Math.Max(img.Width, img.Height) > MaxLongSide)
            {
                var scale = (double)MaxLongSide / Math.Max(img.Width, img.Height);
                img.Mutate(c => c.Resize((int)(img.Width * scale), (int)(img.Height * scale)));
            }

            var work = img;
            Image<Rgb24>? padded = null;
            try
            {
                var faces = m.Detector.DetectFaces(work);
                if (faces.Count == 0)
                {
                    // Ảnh app đã crop sát mặt đôi khi làm SCRFD không bắt được: thêm viền xám rồi thử lại 1 lần.
                    padded = Pad(img);
                    faces = m.Detector.DetectFaces(padded);
                    work = padded;
                }
                if (faces.Count == 0) throw new AppException(400, "Không tìm thấy khuôn mặt");
                if (faces.Count > 1) throw new AppException(400, "Phát hiện nhiều khuôn mặt");

                var landmarks = faces.First().Landmarks;
                if (landmarks == null || landmarks.Count != 5)
                    throw new AppException(400, "Không xác định được các điểm đặc trưng khuôn mặt");

                if (requireOpenEyes) EnsureEyesOpen(m, work, landmarks);

                using var aligned = work.Clone();
                m.Embedder.AlignFaceUsingLandmarks(aligned, landmarks);
                var embedding = m.Embedder.GenerateEmbedding(aligned);
                if (embedding.Length != EmbeddingSize)
                    throw new InvalidOperationException($"Embedding có {embedding.Length} chiều, mong đợi {EmbeddingSize}.");

                var result = (float[])embedding.Clone();
                Normalize(result);
                return result;
            }
            finally
            {
                padded?.Dispose();
            }
        }
    }

    private static Image<Rgb24> Pad(Image<Rgb24> src)
    {
        var padX = (int)(src.Width * 0.4);
        var padY = (int)(src.Height * 0.4);
        var padded = new Image<Rgb24>(src.Width + 2 * padX, src.Height + 2 * padY, new Rgb24(128, 128, 128));
        padded.Mutate(c => c.DrawImage(src, new Point(padX, padY), 1f));
        return padded;
    }

    private static void EnsureEyesOpen(Models m, Image<Rgb24> image, IReadOnlyList<PointF> landmarks)
    {
        var boxes = ImageCalculations.GetEyeBoxesFromCenterPoints(landmarks[0], landmarks[1], 3.0f);
        var bounds = new Rectangle(0, 0, image.Width, image.Height);
        int measured = 0, open = 0;
        foreach (var box in new[] { boxes.Left, boxes.Right })
        {
            var r = Rectangle.Intersect(box, bounds);
            if (r.Width < 16 || r.Height < 16) continue; // mắt quá nhỏ để đánh giá
            using var crop = image.Clone(c => c.Crop(r));
            measured++;
            if (m.Eyes.IsOpen(crop)) open++;
        }
        if (measured > 0 && open == 0)
            throw new AppException(400, "Vui lòng mở mắt khi chụp ảnh");
    }

    private static void Normalize(float[] v)
    {
        double sum = 0;
        foreach (var x in v) sum += (double)x * x;
        var norm = Math.Sqrt(sum);
        if (norm < 1e-9) return;
        for (var i = 0; i < v.Length; i++) v[i] = (float)(v[i] / norm);
    }

    public void Dispose()
    {
        if (_models.IsValueCreated)
        {
            (_models.Value.Detector as IDisposable)?.Dispose();
            (_models.Value.Embedder as IDisposable)?.Dispose();
            (_models.Value.Eyes as IDisposable)?.Dispose();
        }
        _gate.Dispose();
    }
}
