using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NguyenHoaiPhong_1681.Data;
using NguyenHoaiPhong_1681.Entities;
using NguyenHoaiPhong_1681.Enums;
using NguyenHoaiPhong_1681.Helpers;
using NguyenHoaiPhong_1681.Middleware;
using NguyenHoaiPhong_1681.Options;

namespace NguyenHoaiPhong_1681.Services;

public record OtpSendResult(DateTime ExpiresAt, int ResendAfterSeconds);

public interface IOtpService
{
    /// <summary>Tạo phiên OTP mới (chưa gửi mã). Các phiên cũ chưa dùng cùng mục đích của user bị hủy.</summary>
    Task<Guid> CreateSessionAsync(int userId, OtpPurpose purpose);

    /// <summary>Dùng lại phiên còn hiệu lực gần nhất, nếu không có thì tạo mới (để giới hạn gửi lại có tác dụng).</summary>
    Task<Guid> GetOrCreateSessionAsync(int userId, OtpPurpose purpose);

    /// <summary>Gửi (hoặc gửi lại) mã trong phiên. Mã cũ của phiên hết hiệu lực.</summary>
    Task<OtpSendResult> SendAsync(Guid sessionId, OtpPurpose purpose, OtpChannel channel, int? requiredUserId = null);

    /// <summary>Kiểm tra mã. Đúng → trả UserId của phiên; sai/hết hạn → AppException chung.</summary>
    Task<int> VerifyAsync(Guid sessionId, OtpPurpose purpose, string code, int? requiredUserId = null);
}

public class OtpService : IOtpService
{
    private const string GenericError = "Mã không đúng hoặc đã hết hạn";

    private readonly AppDbContext _db;
    private readonly OtpOptions _opt;
    private readonly IEmailSender _email;
    private readonly ISmsSender _sms;
    private readonly ILogger<OtpService> _logger;

    public OtpService(AppDbContext db, IOptions<OtpOptions> opt, IEmailSender email, ISmsSender sms, ILogger<OtpService> logger)
    {
        _db = db;
        _opt = opt.Value;
        _email = email;
        _sms = sms;
        _logger = logger;
    }

    public async Task<Guid> CreateSessionAsync(int userId, OtpPurpose purpose)
    {
        var olds = await _db.OtpCodes.Where(o => o.UserId == userId && o.Purpose == purpose && !o.Used).ToListAsync();
        foreach (var o in olds) o.Used = true;

        var session = new OtpCode
        {
            UserId = userId,
            SessionId = Guid.NewGuid(),
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow // chưa có mã → coi như hết hạn tới khi gửi
        };
        _db.OtpCodes.Add(session);
        await _db.SaveChangesAsync();
        return session.SessionId;
    }

    public async Task<Guid> GetOrCreateSessionAsync(int userId, OtpPurpose purpose)
    {
        var oldest = DateTime.UtcNow.AddMinutes(-_opt.ExpireMinutes * 3);
        var existing = await _db.OtpCodes
            .Where(o => o.UserId == userId && o.Purpose == purpose && !o.Used && o.CreatedAt >= oldest)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
        return existing?.SessionId ?? await CreateSessionAsync(userId, purpose);
    }

    public async Task<OtpSendResult> SendAsync(Guid sessionId, OtpPurpose purpose, OtpChannel channel, int? requiredUserId = null)
    {
        var now = DateTime.UtcNow;
        var otp = await FindSessionAsync(sessionId, purpose, requiredUserId)
            ?? throw new AppException(400, "Phiên xác thực không hợp lệ hoặc đã hết hạn");
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == otp.UserId)
            ?? throw new AppException(400, "Phiên xác thực không hợp lệ hoặc đã hết hạn");

        // Kênh hợp lệ theo mục đích
        string destination, masked;
        switch (channel)
        {
            case OtpChannel.Email when purpose is OtpPurpose.RegisterVerify or OtpPurpose.Login:
                destination = user.Email;
                masked = MaskHelper.MaskEmail(user.Email);
                break;
            case OtpChannel.Sms when purpose == OtpPurpose.PhoneVerify || (purpose == OtpPurpose.Login && user.PhoneVerified):
                if (!PhoneHelper.IsValidE164(user.PhoneNumber))
                    throw new AppException(400, "Số điện thoại không hợp lệ");
                destination = user.PhoneNumber;
                masked = PhoneHelper.Mask(user.PhoneNumber);
                break;
            default:
                throw new AppException(400, "Kênh gửi mã không hợp lệ");
        }

        if (otp.SendCount >= _opt.MaxSendPerSession)
            throw new AppException(429, "Bạn đã gửi mã quá số lần cho phép, vui lòng đăng nhập lại");
        if (otp.LastSentAt is { } last && (now - last).TotalSeconds < _opt.ResendSeconds)
        {
            var wait = (int)Math.Ceiling(_opt.ResendSeconds - (now - last).TotalSeconds);
            throw new AppException(429, $"Vui lòng đợi {wait} giây để gửi lại mã");
        }
        if (channel == OtpChannel.Sms)
        {
            var dayStart = VnTime.ToUtc(VnTime.Now.Date);
            var sentToday = await _db.OtpSendLogs.CountAsync(l =>
                l.UserId == user.Id && l.Channel == OtpChannel.Sms && l.Destination == masked && l.CreatedAt >= dayStart);
            if (sentToday >= _opt.MaxSmsPerPhonePerDay)
                throw new AppException(429, "Số điện thoại này đã đạt giới hạn tin nhắn trong ngày, vui lòng chọn Email");
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        if (_opt.DevMode)
            _logger.LogWarning("[DEV OTP] {Purpose} user={UserId} kênh={Channel}: {Code}", purpose, user.Id, channel, code);

        try
        {
            if (channel == OtpChannel.Email) await _email.SendOtpAsync(destination, code);
            else await _sms.SendOtpAsync(destination, code);
        }
        catch (AppException ex)
        {
            otp.SendCount++;
            _db.OtpSendLogs.Add(new OtpSendLog { UserId = user.Id, Channel = channel, Destination = masked, Success = false, Error = ex.Message });
            await _db.SaveChangesAsync();
            throw;
        }

        otp.Channel = channel;
        otp.CodeHash = HashCode(sessionId, code);
        otp.ExpiresAt = now.AddMinutes(_opt.ExpireMinutes);
        otp.Attempts = 0;
        otp.SendCount++;
        otp.LastSentAt = now;
        _db.OtpSendLogs.Add(new OtpSendLog { UserId = user.Id, Channel = channel, Destination = masked, Success = true });
        await _db.SaveChangesAsync();

        return new OtpSendResult(otp.ExpiresAt, _opt.ResendSeconds);
    }

    public async Task<int> VerifyAsync(Guid sessionId, OtpPurpose purpose, string code, int? requiredUserId = null)
    {
        var now = DateTime.UtcNow;
        var otp = await FindSessionAsync(sessionId, purpose, requiredUserId);
        if (otp?.CodeHash == null || otp.ExpiresAt <= now) throw new AppException(400, GenericError);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == otp.UserId);
        if (user == null || (user.LockoutUntil.HasValue && user.LockoutUntil > now))
            throw new AppException(400, GenericError);

        if (!HashHelper.FixedEquals(otp.CodeHash, HashCode(sessionId, code)))
        {
            otp.Attempts++;
            user.FailedOtpCount++;
            if (user.FailedOtpCount >= _opt.MaxAttempts)
            {
                user.LockoutUntil = now.AddMinutes(_opt.LockMinutes);
                user.FailedOtpCount = 0;
                otp.Used = true;
            }
            await _db.SaveChangesAsync();
            throw new AppException(400, GenericError);
        }

        otp.Used = true;
        user.FailedOtpCount = 0;
        await _db.SaveChangesAsync();
        return user.Id;
    }

    private async Task<OtpCode?> FindSessionAsync(Guid sessionId, OtpPurpose purpose, int? requiredUserId)
    {
        // Phiên chỉ có hiệu lực một khoảng ngắn kể từ lúc tạo.
        var oldest = DateTime.UtcNow.AddMinutes(-_opt.ExpireMinutes * 3);
        return await _db.OtpCodes.FirstOrDefaultAsync(o =>
            o.SessionId == sessionId && o.Purpose == purpose && !o.Used && o.CreatedAt >= oldest &&
            (requiredUserId == null || o.UserId == requiredUserId));
    }

    private static string HashCode(Guid sessionId, string code) => HashHelper.Sha256Hex($"{sessionId:N}:{code}");
}
