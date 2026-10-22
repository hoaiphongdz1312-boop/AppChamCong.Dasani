using System.ComponentModel.DataAnnotations;
using NguyenHoaiPhong_1681.Enums;

namespace NguyenHoaiPhong_1681.DTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    [RegularExpression(@"^[a-zA-Z0-9_.]{3,32}$", ErrorMessage = "Tên đăng nhập gồm 3-32 ký tự chữ, số, dấu chấm hoặc gạch dưới")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{8,}$", ErrorMessage = "Mật khẩu tối thiểu 8 ký tự, gồm cả chữ và số")]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(128)]
    public string FullName { get; set; } = string.Empty;
}

public class OtpSessionResponse
{
    public Guid OtpSessionId { get; set; }
}

public class VerifyRegisterRequest
{
    [Required] public Guid OtpSessionId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã gồm 6 chữ số")]
    public string Code { get; set; } = string.Empty;
}

public class VerifyRegisterResponse
{
    public bool EmailVerified { get; set; }
    public UserStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")] public string Username { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")] public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public Guid OtpSessionId { get; set; }
    public string[] AvailableChannels { get; set; } = Array.Empty<string>();
    public string MaskedEmail { get; set; } = string.Empty;
    public string MaskedPhone { get; set; } = string.Empty;
}

public class SendOtpRequest
{
    [Required] public Guid OtpSessionId { get; set; }
    [Required] public OtpChannel Channel { get; set; }
}

public class SendOtpResponse
{
    public DateTime ExpiresAt { get; set; }
    public int ResendAfterSeconds { get; set; }
}

public class VerifyLoginOtpRequest
{
    [Required] public Guid OtpSessionId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã gồm 6 chữ số")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Thiếu deviceId")]
    [StringLength(128)]
    public string DeviceId { get; set; } = string.Empty;
}

public class GoogleLoginRequest
{
    [Required] public string IdToken { get; set; } = string.Empty;
    [Required, StringLength(128)] public string DeviceId { get; set; } = string.Empty;
}

public class RefreshRequest
{
    [Required] public string RefreshToken { get; set; } = string.Empty;
}

public class VerifyPhoneRequest
{
    [Required] public Guid OtpSessionId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã gồm 6 chữ số")]
    public string Code { get; set; } = string.Empty;
}

public class AuthUserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public Role Role { get; set; }
    public int? DepartmentId { get; set; }
    public bool HasFaceEnrolled { get; set; }
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public AuthUserDto User { get; set; } = new();
}
