using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NguyenHoaiPhong_1681.Data;
using NguyenHoaiPhong_1681.Entities;
using NguyenHoaiPhong_1681.Enums;
using NguyenHoaiPhong_1681.Helpers;
using NguyenHoaiPhong_1681.Middleware;
using NguyenHoaiPhong_1681.Options;

namespace NguyenHoaiPhong_1681.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user);

    /// <summary>Tạo refresh token mới (chỉ lưu hash) và trả chuỗi gốc cho client.</summary>
    Task<string> CreateRefreshTokenAsync(User user, string deviceId);

    /// <summary>Kiểm tra refresh token, thu hồi token cũ (rotate) và trả user + deviceId.</summary>
    Task<(User User, string DeviceId)> ConsumeRefreshTokenAsync(string refreshToken);

    Task RevokeAsync(string refreshToken, int userId);
}

public class TokenService : ITokenService
{
    private readonly AppDbContext _db;
    private readonly JwtOptions _jwt;

    public TokenService(AppDbContext db, IOptions<JwtOptions> jwt)
    {
        _db = db;
        _jwt = jwt.Value;
    }

    public (string Token, DateTime ExpiresAt) CreateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new("unique_name", user.Username),
            new("role", user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        if (user.DepartmentId.HasValue)
            claims.Add(new Claim("departmentId", user.DepartmentId.Value.ToString()));

        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessMinutes);
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, DateTime.UtcNow, expires, creds);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public async Task<string> CreateRefreshTokenAsync(User user, string deviceId)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashHelper.Sha256Hex(raw),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshDays),
            DeviceId = deviceId
        });
        await _db.SaveChangesAsync();
        return raw;
    }

    public async Task<(User User, string DeviceId)> ConsumeRefreshTokenAsync(string refreshToken)
    {
        var invalid = new AppException(401, "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.");
        var hash = HashHelper.Sha256Hex(refreshToken);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (token == null || !HashHelper.FixedEquals(token.TokenHash, hash)) throw invalid;

        if (token.RevokedAt != null)
        {
            // Token đã bị dùng lại → nghi bị đánh cắp, thu hồi toàn bộ token còn hiệu lực của user.
            var active = await _db.RefreshTokens.Where(t => t.UserId == token.UserId && t.RevokedAt == null).ToListAsync();
            foreach (var t in active) t.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            throw invalid;
        }
        if (token.ExpiresAt <= DateTime.UtcNow) throw invalid;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == token.UserId);
        if (user == null) throw invalid;
        if (user.Status != UserStatus.Active)
            throw new AppException(403, user.Status == UserStatus.Locked ? "Tài khoản đã bị khóa" : "Tài khoản đang chờ duyệt");

        token.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (user, token.DeviceId);
    }

    public async Task RevokeAsync(string refreshToken, int userId)
    {
        var hash = HashHelper.Sha256Hex(refreshToken);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.UserId == userId);
        if (token is { RevokedAt: null })
        {
            token.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
