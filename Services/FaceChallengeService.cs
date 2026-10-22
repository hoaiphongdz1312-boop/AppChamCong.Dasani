using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using NguyenHoaiPhong_1681.Data;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Entities;
using NguyenHoaiPhong_1681.Middleware;

namespace NguyenHoaiPhong_1681.Services;

public interface IFaceChallengeService
{
    Task<FaceChallengeResponse> CreateAsync(string? username);

    /// <summary>Dùng challenge đúng 1 lần. Hết hạn / đã dùng / sai người dùng → AppException 400.</summary>
    Task ConsumeAsync(Guid token, int? userId);
}

public class FaceChallengeService : IFaceChallengeService
{
    private const int TtlSeconds = 30;
    private static readonly string[] AllActions = { "blink", "turn_left", "turn_right", "smile" };

    private readonly AppDbContext _db;

    public FaceChallengeService(AppDbContext db) => _db = db;

    public async Task<FaceChallengeResponse> CreateAsync(string? username)
    {
        int? userId = null;
        if (!string.IsNullOrWhiteSpace(username))
        {
            var name = username.Trim();
            // Không báo lỗi khi username không tồn tại để tránh dò tài khoản.
            userId = await _db.Users.Where(u => u.Username == name).Select(u => (int?)u.Id).FirstOrDefaultAsync();
        }

        // Chọn 2 action khác nhau, ngẫu nhiên (Fisher-Yates trên bản sao).
        var pool = (string[])AllActions.Clone();
        for (var i = pool.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        var actions = pool.Take(2).ToArray();

        var challenge = new FaceChallenge
        {
            UserId = userId,
            Token = Guid.NewGuid(),
            Actions = string.Join(',', actions),
            ExpiresAt = DateTime.UtcNow.AddSeconds(TtlSeconds)
        };
        _db.FaceChallenges.Add(challenge);
        await _db.SaveChangesAsync();

        // Dọn challenge cũ.
        var cutoff = DateTime.UtcNow.AddHours(-1);
        await _db.FaceChallenges.Where(c => c.ExpiresAt < cutoff).ExecuteDeleteAsync();

        return new FaceChallengeResponse { ChallengeToken = challenge.Token, Actions = actions, ExpiresAt = challenge.ExpiresAt };
    }

    public async Task ConsumeAsync(Guid token, int? userId)
    {
        var now = DateTime.UtcNow;
        // Cập nhật nguyên tử để một challenge không thể được dùng hai lần song song.
        var updated = await _db.FaceChallenges
            .Where(c => c.Token == token && !c.Used && c.ExpiresAt > now && (c.UserId == null || c.UserId == userId))
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Used, true));
        if (updated == 1) return;

        var existing = await _db.FaceChallenges.AsNoTracking().FirstOrDefaultAsync(c => c.Token == token);
        if (existing == null) throw new AppException(400, "Mã thử thách không hợp lệ, vui lòng thử lại");
        if (existing.Used) throw new AppException(400, "Mã thử thách đã được sử dụng, vui lòng thử lại");
        if (existing.ExpiresAt <= now) throw new AppException(400, "Mã thử thách đã hết hạn, vui lòng thử lại");
        throw new AppException(400, "Mã thử thách không thuộc về tài khoản này");
    }
}
