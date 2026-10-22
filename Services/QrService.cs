using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NguyenHoaiPhong_1681.Data;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Entities;
using NguyenHoaiPhong_1681.Middleware;
using NguyenHoaiPhong_1681.Options;
using QRCoder;

namespace NguyenHoaiPhong_1681.Services;

public interface IQrService
{
    /// <summary>Số thứ tự khung thời gian hiện tại = floor(UnixSeconds / WindowSeconds).</summary>
    long CurrentWindow();

    /// <summary>Tạo token QR của khung hiện tại cho địa điểm. Không lưu DB.</summary>
    QrResponse Generate(WorkLocation location);

    /// <summary>Dành cho trang kiosk: kiểm tra KioskKey rồi tạo QR. Sai key hoặc không có địa điểm → 401.</summary>
    Task<QrResponse> GetForKioskAsync(int locationId, string? key);

    /// <summary>Dành cho quản lý mở QR trên app.</summary>
    Task<QrResponse> GetCurrentAsync(int locationId);

    /// <summary>
    /// Kiểm tra token thuộc đúng địa điểm, thuộc khung hiện tại hoặc khung liền trước, chữ ký hợp lệ.
    /// Trả về window của token; sai/hết hạn → AppException 400.
    /// </summary>
    Task<long> ValidateAsync(string token, int expectedLocationId);

    /// <summary>Vẽ token thành ảnh PNG (ECC M, 20 px/module) mã hóa base64.</summary>
    string RenderPng(string token);
}

public class QrService : IQrService
{
    private const string ExpiredMessage = "Mã QR đã hết hạn, vui lòng quét lại mã đang hiển thị";

    private readonly AppDbContext _db;
    private readonly int _windowSeconds;

    public QrService(AppDbContext db, IOptions<QrOptions> options)
    {
        _db = db;
        _windowSeconds = Math.Max(options.Value.WindowSeconds, 5);
    }

    public long CurrentWindow() => DateTimeOffset.UtcNow.ToUnixTimeSeconds() / _windowSeconds;

    public QrResponse Generate(WorkLocation location)
    {
        var window = CurrentWindow();
        var token = QrTokenCodec.Create(location.Id, window, location.QrSecret);
        var expires = DateTimeOffset.FromUnixTimeSeconds((window + 1) * _windowSeconds).UtcDateTime;
        return new QrResponse
        {
            Token = token,
            ExpiresAt = expires,
            SecondsLeft = Math.Max(0, (int)Math.Ceiling((expires - DateTime.UtcNow).TotalSeconds)),
            WindowSeconds = _windowSeconds,
            LocationName = location.Name,
            QrPngBase64 = RenderPng(token)
        };
    }

    public async Task<QrResponse> GetForKioskAsync(int locationId, string? key)
    {
        var invalid = new AppException(401, "Đường dẫn kiosk không hợp lệ");
        var location = await _db.WorkLocations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == locationId && l.IsActive);
        if (location == null || string.IsNullOrEmpty(key)) throw invalid;

        var ok = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(location.KioskKey), Encoding.UTF8.GetBytes(key));
        if (!ok) throw invalid;
        return Generate(location);
    }

    public async Task<QrResponse> GetCurrentAsync(int locationId)
    {
        var location = await _db.WorkLocations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == locationId && l.IsActive)
                       ?? throw new AppException(404, "Không tìm thấy địa điểm làm việc");
        return Generate(location);
    }

    public async Task<long> ValidateAsync(string token, int expectedLocationId)
    {
        var expired = new AppException(400, ExpiredMessage);
        if (!QrTokenCodec.TryParse(token, out var locationId, out var window, out var mac)) throw expired;
        if (locationId != expectedLocationId) throw expired;

        // Chịu lệch đồng hồ: chấp nhận khung hiện tại và khung liền trước.
        var current = CurrentWindow();
        if (window != current && window != current - 1) throw expired;

        var secret = await _db.WorkLocations.AsNoTracking()
            .Where(l => l.Id == locationId && l.IsActive)
            .Select(l => l.QrSecret).FirstOrDefaultAsync();
        if (secret == null || !QrTokenCodec.VerifyMac(locationId, window, mac, secret)) throw expired;
        return window;
    }

    public string RenderPng(string token)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(token, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data).GetGraphic(20);
        return Convert.ToBase64String(png);
    }
}
