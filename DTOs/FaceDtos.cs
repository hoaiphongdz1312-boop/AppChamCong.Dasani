using System.ComponentModel.DataAnnotations;

namespace NguyenHoaiPhong_1681.DTOs;

public class FaceChallengeRequest
{
    /// <summary>Tùy chọn: gắn challenge với một tài khoản (dùng cho face-login).</summary>
    [StringLength(64)] public string? Username { get; set; }
}

public class FaceChallengeResponse
{
    public Guid ChallengeToken { get; set; }
    public string[] Actions { get; set; } = Array.Empty<string>();
    public DateTime ExpiresAt { get; set; }
}

public class FaceEnrollRequest
{
    [Required(ErrorMessage = "Thiếu ảnh khuôn mặt")] public List<IFormFile> Images { get; set; } = new();
    [Required] public Guid ChallengeToken { get; set; }
}

public class FaceEnrollResponse
{
    public int SampleCount { get; set; }
    public bool HasFaceEnrolled { get; set; } = true;
}

public class FaceVerifyRequest
{
    [Required(ErrorMessage = "Thiếu ảnh khuôn mặt")] public IFormFile? Image { get; set; }
    [Required] public Guid ChallengeToken { get; set; }
    public bool LivenessPassed { get; set; }
}

public class FaceVerifyResponse
{
    public double Score { get; set; }
    public bool Matched { get; set; }
    public bool Retry { get; set; }
}

public class FaceLoginRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")] public string Username { get; set; } = string.Empty;
    [Required(ErrorMessage = "Thiếu ảnh khuôn mặt")] public IFormFile? Image { get; set; }
    [Required] public Guid ChallengeToken { get; set; }
    public bool LivenessPassed { get; set; }
    [Required(ErrorMessage = "Thiếu deviceId"), StringLength(128)] public string DeviceId { get; set; } = string.Empty;
}
