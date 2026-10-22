using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Helpers;
using NguyenHoaiPhong_1681.Middleware;
using NguyenHoaiPhong_1681.Services;

namespace NguyenHoaiPhong_1681.Controllers;

[ApiController]
[Route("api/face")]
public class FaceController : ControllerBase
{
    private readonly IFaceService _face;
    private readonly IFaceChallengeService _challenges;

    public FaceController(IFaceService face, IFaceChallengeService challenges)
    {
        _face = face;
        _challenges = challenges;
    }

    /// <summary>Tạo thử thách liveness (2 hành động ngẫu nhiên, hiệu lực 30 giây).</summary>
    [HttpPost("challenge")]
    [AllowAnonymous]
    [EnableRateLimiting("challenge")]
    public async Task<ActionResult<FaceChallengeResponse>> Challenge(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] FaceChallengeRequest? req) =>
        Ok(await _challenges.CreateAsync(req?.Username));

    /// <summary>Đăng ký khuôn mặt từ 3–5 ảnh JPEG/PNG đã crop mặt. Ảnh không được lưu, chỉ lưu vector.</summary>
    [HttpPost("enroll")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<FaceEnrollResponse>> Enroll([FromForm] FaceEnrollRequest req)
    {
        if (req.Images.Count is < 3 or > 5) throw new AppException(400, "Cần gửi từ 3 đến 5 ảnh khuôn mặt");
        foreach (var f in req.Images) ValidateFile(f);

        var userId = User.GetUserId();
        await _challenges.ConsumeAsync(req.ChallengeToken, userId);

        var streams = new List<Stream>();
        try
        {
            foreach (var f in req.Images) streams.Add(f.OpenReadStream());
            var count = await _face.EnrollAsync(userId, streams);
            return Ok(new FaceEnrollResponse { SampleCount = count });
        }
        finally
        {
            foreach (var s in streams) await s.DisposeAsync();
        }
    }

    /// <summary>So khớp 1 ảnh với khuôn mặt đã đăng ký của tài khoản hiện tại.</summary>
    [HttpPost("verify")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<FaceVerifyResponse>> Verify([FromForm] FaceVerifyRequest req)
    {
        ValidateFile(req.Image);
        var userId = User.GetUserId();
        await _challenges.ConsumeAsync(req.ChallengeToken, userId);

        await using var stream = req.Image!.OpenReadStream();
        var r = await _face.VerifyUserAsync(userId, stream);

        // Liveness thất bại thì không bao giờ coi là khớp.
        if (!req.LivenessPassed)
            return Ok(new FaceVerifyResponse { Score = r.Score, Matched = false, Retry = false });
        return Ok(new FaceVerifyResponse { Score = r.Score, Matched = r.Matched, Retry = r.Retry });
    }

    internal static void ValidateFile(IFormFile? file)
    {
        if (file == null || file.Length == 0) throw new AppException(400, "Thiếu ảnh khuôn mặt");
        if (file.Length > FaceService.MaxImageBytes) throw new AppException(400, "Ảnh quá lớn (tối đa 2MB)");
    }
}
