using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Helpers;
using NguyenHoaiPhong_1681.Services;

namespace NguyenHoaiPhong_1681.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    // <summary>Đăng ký tài khoản, gửi OTP xác minh tới email.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(OtpSessionResponse), 200)]
    public async Task<IActionResult> Register(RegisterRequest req) => Ok(await _auth.RegisterAsync(req));

    /// <summary>Xác minh email bằng OTP; tài khoản vẫn chờ duyệt trừ khi bật AutoApprove.</summary>
    [HttpPost("register/verify")]
    public async Task<ActionResult<VerifyRegisterResponse>> VerifyRegister(VerifyRegisterRequest req) =>
        Ok(await _auth.VerifyRegisterAsync(req));

    /// <summary>Bước 1 đăng nhập: kiểm tra mật khẩu, trả phiên OTP và các kênh khả dụng (chưa cấp token).</summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest req) => Ok(await _auth.LoginAsync(req));

    /// <summary>Gửi OTP đăng nhập qua Email hoặc Sms.</summary>
    [HttpPost("login/send-otp")]
    public async Task<ActionResult<SendOtpResponse>> LoginSendOtp(SendOtpRequest req) =>
        Ok(await _auth.LoginSendOtpAsync(req));

    /// <summary>Bước 2 đăng nhập: xác minh OTP và cấp access/refresh token.</summary>
    [HttpPost("login/verify-otp")]
    public async Task<ActionResult<AuthResponse>> LoginVerifyOtp(VerifyLoginOtpRequest req) =>
        Ok(await _auth.LoginVerifyOtpAsync(req));

    /// <summary>Đăng nhập bằng Google ID token.</summary>
    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> Google(GoogleLoginRequest req) =>
        Ok(await _auth.GoogleLoginAsync(req));

    /// <summary>Đăng nhập bằng khuôn mặt (so khớp 1:1 với tài khoản đã đăng ký khuôn mặt).</summary>
    [HttpPost("face-login")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AuthResponse>> FaceLogin([FromForm] FaceLoginRequest req)
    {
        FaceController.ValidateFile(req.Image);
        await using var stream = req.Image!.OpenReadStream();
        return Ok(await _auth.FaceLoginAsync(req, stream));
    }

    /// <summary>Đổi refresh token lấy cặp token mới (token cũ bị thu hồi).</summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest req) => Ok(await _auth.RefreshAsync(req));

    /// <summary>Đăng xuất: thu hồi refresh token.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(RefreshRequest req)
    {
        await _auth.LogoutAsync(User.GetUserId(), req);
        return NoContent();
    }

    /// <summary>Gửi SMS OTP để xác minh số điện thoại của tài khoản hiện tại.</summary>
    [HttpPost("phone/send-otp")]
    [Authorize]
    public async Task<ActionResult<OtpSessionResponse>> PhoneSendOtp() =>
        Ok(await _auth.PhoneSendOtpAsync(User.GetUserId()));

    /// <summary>Xác minh số điện thoại bằng mã SMS.</summary>
    [HttpPost("phone/verify")]
    [Authorize]
    public async Task<IActionResult> PhoneVerify(VerifyPhoneRequest req)
    {
        await _auth.PhoneVerifyAsync(User.GetUserId(), req);
        return Ok(new { phoneVerified = true });
    }
}
