using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NguyenHoaiPhong_1681.Data;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Entities;
using NguyenHoaiPhong_1681.Enums;
using NguyenHoaiPhong_1681.Helpers;
using NguyenHoaiPhong_1681.Middleware;
using NguyenHoaiPhong_1681.Options;

namespace NguyenHoaiPhong_1681.Services;

public interface IAuthService
{
    Task<OtpSessionResponse> RegisterAsync(RegisterRequest req);
    Task<VerifyRegisterResponse> VerifyRegisterAsync(VerifyRegisterRequest req);
    Task<LoginResponse> LoginAsync(LoginRequest req);
    Task<SendOtpResponse> LoginSendOtpAsync(SendOtpRequest req);
    Task<AuthResponse> LoginVerifyOtpAsync(VerifyLoginOtpRequest req);
    Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest req);
    Task<AuthResponse> FaceLoginAsync(FaceLoginRequest req, Stream image);
    Task<AuthResponse> RefreshAsync(RefreshRequest req);
    Task LogoutAsync(int userId, RefreshRequest req);
    Task<OtpSessionResponse> PhoneSendOtpAsync(int userId);
    Task PhoneVerifyAsync(int userId, VerifyPhoneRequest req);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokens;
    private readonly IOtpService _otp;
    private readonly AuthOptions _auth;
    private readonly GoogleOptions _google;
    private readonly OtpOptions _otpOpt;
    private readonly IFaceService _face;
    private readonly IFaceChallengeService _challenges;

    public AuthService(AppDbContext db, ITokenService tokens, IOtpService otp,
        IOptions<AuthOptions> auth, IOptions<GoogleOptions> google, IOptions<OtpOptions> otpOpt,
        IFaceService face, IFaceChallengeService challenges)
    {
        _face = face;
        _challenges = challenges;
        _db = db;
        _tokens = tokens;
        _otp = otp;
        _auth = auth.Value;
        _google = google.Value;
        _otpOpt = otpOpt.Value;
    }

    public async Task<OtpSessionResponse> RegisterAsync(RegisterRequest req)
    {
        var username = req.Username.Trim();
        var email = req.Email.Trim().ToLowerInvariant();
        var phone = PhoneHelper.Normalize(req.Phone);
        if (!PhoneHelper.IsValidE164(phone))
            throw new AppException(400, "Số điện thoại không hợp lệ");

        // IgnoreQueryFilters: unique index áp dụng cả với user đã xóa mềm.
        if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == username))
            throw new AppException(409, "Tên đăng nhập đã tồn tại");
        if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email))
            throw new AppException(409, "Email đã được sử dụng");

        var user = new User
        {
            Username = username,
            Email = email,
            PhoneNumber = phone,
            FullName = req.FullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = Role.Employee,
            Status = UserStatus.PendingApproval,
            EmailVerified = false
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var sessionId = await _otp.CreateSessionAsync(user.Id, OtpPurpose.RegisterVerify);
        await _otp.SendAsync(sessionId, OtpPurpose.RegisterVerify, OtpChannel.Email);
        return new OtpSessionResponse { OtpSessionId = sessionId };
    }

    public async Task<VerifyRegisterResponse> VerifyRegisterAsync(VerifyRegisterRequest req)
    {
        var userId = await _otp.VerifyAsync(req.OtpSessionId, OtpPurpose.RegisterVerify, req.Code);
        var user = await _db.Users.FirstAsync(u => u.Id == userId);
        user.EmailVerified = true;
        if (_auth.AutoApprove && user.Status == UserStatus.PendingApproval)
            user.Status = UserStatus.Active;
        await _db.SaveChangesAsync();

        return new VerifyRegisterResponse
        {
            EmailVerified = true,
            Status = user.Status,
            Message = user.Status == UserStatus.Active
                ? "Xác minh email thành công, bạn có thể đăng nhập."
                : "Xác minh email thành công, vui lòng chờ quản trị viên duyệt tài khoản."
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest req)
    {
        var username = req.Username.Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user?.PasswordHash == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new AppException(401, "Tên đăng nhập hoặc mật khẩu không đúng");

        if (user.LockoutUntil is { } until && until > DateTime.UtcNow)
        {
            var minutes = (int)Math.Ceiling((until - DateTime.UtcNow).TotalMinutes);
            throw new AppException(423, $"Tài khoản tạm khóa do nhập sai mã nhiều lần, vui lòng thử lại sau {minutes} phút");
        }
        EnsureActive(user);

        var sessionId = await _otp.CreateSessionAsync(user.Id, OtpPurpose.Login);
        var channels = new List<string> { nameof(OtpChannel.Email) };
        if (user.PhoneVerified) channels.Add(nameof(OtpChannel.Sms));

        return new LoginResponse
        {
            OtpSessionId = sessionId,
            AvailableChannels = channels.ToArray(),
            MaskedEmail = MaskHelper.MaskEmail(user.Email),
            MaskedPhone = user.PhoneVerified ? PhoneHelper.Mask(user.PhoneNumber) : string.Empty
        };
    }

    public async Task<SendOtpResponse> LoginSendOtpAsync(SendOtpRequest req)
    {
        var result = await _otp.SendAsync(req.OtpSessionId, OtpPurpose.Login, req.Channel);
        return new SendOtpResponse { ExpiresAt = result.ExpiresAt, ResendAfterSeconds = result.ResendAfterSeconds };
    }

    public async Task<AuthResponse> LoginVerifyOtpAsync(VerifyLoginOtpRequest req)
    {
        var userId = await _otp.VerifyAsync(req.OtpSessionId, OtpPurpose.Login, req.Code);
        var user = await _db.Users.FirstAsync(u => u.Id == userId);
        EnsureActive(user);
        return await IssueAsync(user, req.DeviceId);
    }

    public async Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(_google.WebClientId))
            throw new AppException(503, "Máy chủ chưa cấu hình đăng nhập Google");

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(req.IdToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { _google.WebClientId } });
        }
        catch (InvalidJwtException)
        {
            throw new AppException(401, "Đăng nhập Google không hợp lệ");
        }
        if (string.IsNullOrEmpty(payload.Email) || payload.EmailVerified != true)
            throw new AppException(401, "Email Google chưa được xác minh");

        var email = payload.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.GoogleSubjectId == payload.Subject)
                   ?? await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email);

        if (user is { IsDeleted: true })
            throw new AppException(403, "Tài khoản không khả dụng");

        if (user == null)
        {
            user = new User
            {
                Username = await GenerateUsernameAsync(email),
                Email = email,
                PhoneNumber = string.Empty,
                FullName = string.IsNullOrWhiteSpace(payload.Name) ? email : payload.Name,
                AvatarUrl = payload.Picture,
                GoogleSubjectId = payload.Subject,
                Role = Role.Employee,
                Status = _auth.AutoApprove ? UserStatus.Active : UserStatus.PendingApproval,
                EmailVerified = true
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        else if (user.GoogleSubjectId == null)
        {
            user.GoogleSubjectId = payload.Subject;
            user.EmailVerified = true;
            await _db.SaveChangesAsync();
        }

        EnsureActive(user);
        return await IssueAsync(user, req.DeviceId);
    }

    public async Task<AuthResponse> FaceLoginAsync(FaceLoginRequest req, Stream image)
    {
        const string failMessage = "Xác thực khuôn mặt không thành công";
        var username = req.Username.Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);

        // Challenge được gắn với user nếu username tồn tại → dùng sai người sẽ bị từ chối.
        await _challenges.ConsumeAsync(req.ChallengeToken, user?.Id);

        if (user == null || !await _db.FaceEmbeddings.AnyAsync(f => f.UserId == user.Id))
            throw new AppException(401, failMessage) { Extra = new() { ["retry"] = false } };
        if (user.LockoutUntil is { } until && until > DateTime.UtcNow)
            throw new AppException(401, failMessage) { Extra = new() { ["retry"] = false } };

        var result = await _face.VerifyUserAsync(user.Id, image);
        if (!req.LivenessPassed || !result.Matched)
            throw new AppException(401, failMessage)
            {
                Extra = new() { ["retry"] = req.LivenessPassed && result.Retry, ["score"] = result.Score }
            };

        EnsureActive(user);
        return await IssueAsync(user, req.DeviceId);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest req)
    {
        var (user, deviceId) = await _tokens.ConsumeRefreshTokenAsync(req.RefreshToken);
        return await IssueAsync(user, deviceId);
    }

    public Task LogoutAsync(int userId, RefreshRequest req) => _tokens.RevokeAsync(req.RefreshToken, userId);

    public async Task<OtpSessionResponse> PhoneSendOtpAsync(int userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
                   ?? throw new AppException(401, "Phiên đăng nhập không hợp lệ.");
        if (!PhoneHelper.IsValidE164(user.PhoneNumber))
            throw new AppException(400, "Bạn chưa có số điện thoại hợp lệ, vui lòng cập nhật hồ sơ");
        if (user.PhoneVerified)
            throw new AppException(400, "Số điện thoại đã được xác minh");

        var sessionId = await _otp.GetOrCreateSessionAsync(userId, OtpPurpose.PhoneVerify);
        await _otp.SendAsync(sessionId, OtpPurpose.PhoneVerify, OtpChannel.Sms, userId);
        return new OtpSessionResponse { OtpSessionId = sessionId };
    }

    public async Task PhoneVerifyAsync(int userId, VerifyPhoneRequest req)
    {
        await _otp.VerifyAsync(req.OtpSessionId, OtpPurpose.PhoneVerify, req.Code, userId);
        var user = await _db.Users.FirstAsync(u => u.Id == userId);
        user.PhoneVerified = true;
        await _db.SaveChangesAsync();
    }

    private static void EnsureActive(User user)
    {
        if (user.Status == UserStatus.PendingApproval) throw new AppException(403, "Tài khoản đang chờ duyệt");
        if (user.Status == UserStatus.Locked) throw new AppException(403, "Tài khoản đã bị khóa");
    }

    private async Task<AuthResponse> IssueAsync(User user, string deviceId)
    {
        var (access, expires) = _tokens.CreateAccessToken(user);
        var refresh = await _tokens.CreateRefreshTokenAsync(user, deviceId);
        return new AuthResponse
        {
            AccessToken = access,
            RefreshToken = refresh,
            ExpiresAt = expires,
            User = new AuthUserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Role = user.Role,
                DepartmentId = user.DepartmentId,
                HasFaceEnrolled = await _db.FaceEmbeddings.AnyAsync(f => f.UserId == user.Id)
            }
        };
    }

    private async Task<string> GenerateUsernameAsync(string email)
    {
        var baseName = new string(email.Split('@')[0].Where(c => char.IsLetterOrDigit(c) || c is '.' or '_').ToArray());
        if (baseName.Length < 3) baseName = "user" + baseName;
        if (baseName.Length > 28) baseName = baseName[..28];

        var name = baseName;
        var i = 1;
        while (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == name))
            name = $"{baseName}{i++}";
        return name;
    }
}
