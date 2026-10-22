using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NguyenHoaiPhong_1681.Data;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Entities;
using NguyenHoaiPhong_1681.Helpers;
using NguyenHoaiPhong_1681.Middleware;

namespace NguyenHoaiPhong_1681.Services;

public interface ILocationService
{
    Task ShareAsync(ClaimsPrincipal user, ShareLocationRequest req);
    Task<List<LiveLocationDto>> LiveAsync(ClaimsPrincipal user);
    Task<List<LocationPointDto>> HistoryAsync(ClaimsPrincipal user, int userId, DateOnly? date);
}

public class LocationService : ILocationService
{
    private static readonly TimeSpan LiveWindow = TimeSpan.FromMinutes(5);
    private const int MaxHistoryPoints = 3000;
    private const int RetentionDays = 30;

    private readonly AppDbContext _db;

    public LocationService(AppDbContext db) => _db = db;

    public async Task ShareAsync(ClaimsPrincipal principal, ShareLocationRequest req)
    {
        var userId = principal.GetUserId();
        var now = DateTime.UtcNow;
        _db.LocationShares.Add(new LocationShare
        {
            UserId = userId,
            Latitude = req.Latitude,
            Longitude = req.Longitude,
            Accuracy = req.Accuracy,
            Timestamp = now,
            IsSharing = req.IsSharing
        });
        await _db.SaveChangesAsync();

        // Dọn điểm quá cũ của chính người dùng để bảng không phình mãi (app gửi mỗi 30 giây).
        var cutoff = now.AddDays(-RetentionDays);
        await _db.LocationShares.Where(l => l.UserId == userId && l.Timestamp < cutoff).ExecuteDeleteAsync();
    }

    public async Task<List<LiveLocationDto>> LiveAsync(ClaimsPrincipal principal)
    {
        var actor = await Actor.FromAsync(principal, _db);
        var since = DateTime.UtcNow - LiveWindow;

        // Chỉ lấy điểm MỚI NHẤT của mỗi người; nếu điểm mới nhất là "tắt chia sẻ" thì không hiển thị.
        var query = _db.LocationShares.AsNoTracking()
            .Where(l => l.Id == _db.LocationShares
                .Where(x => x.UserId == l.UserId)
                .OrderByDescending(x => x.Timestamp).ThenByDescending(x => x.Id)
                .Select(x => x.Id).FirstOrDefault())
            .Where(l => l.IsSharing && l.Timestamp >= since);

        if (!actor.IsAdmin)
        {
            if (actor.DepartmentId == null) throw new AppException(403, "Bạn chưa thuộc phòng ban nào.");
            query = query.Where(l => l.User!.DepartmentId == actor.DepartmentId);
        }

        return await query.OrderBy(l => l.User!.FullName)
            .Select(l => new LiveLocationDto
            {
                UserId = l.UserId, FullName = l.User!.FullName, Latitude = l.Latitude,
                Longitude = l.Longitude, Accuracy = l.Accuracy, Timestamp = l.Timestamp
            })
            .ToListAsync();
    }

    public async Task<List<LocationPointDto>> HistoryAsync(ClaimsPrincipal principal, int userId, DateOnly? date)
    {
        var actor = await Actor.FromAsync(principal, _db);
        if (!actor.IsAdmin && actor.Id != userId)
        {
            var dept = await _db.Users.Where(u => u.Id == userId).Select(u => u.DepartmentId).FirstOrDefaultAsync();
            actor.EnsureCanManage(dept, "Bạn chỉ được xem lịch sử vị trí của chính mình hoặc nhân viên cùng phòng ban.");
        }

        var day = date ?? VnTime.Today;
        var start = VnTime.ToUtc(day.ToDateTime(TimeOnly.MinValue));
        var end = VnTime.ToUtc(day.AddDays(1).ToDateTime(TimeOnly.MinValue));
        return await _db.LocationShares.AsNoTracking()
            .Where(l => l.UserId == userId && l.Timestamp >= start && l.Timestamp < end)
            .OrderBy(l => l.Timestamp)
            .Take(MaxHistoryPoints)
            .Select(l => new LocationPointDto
            {
                Latitude = l.Latitude, Longitude = l.Longitude, Accuracy = l.Accuracy,
                Timestamp = l.Timestamp, IsSharing = l.IsSharing
            })
            .ToListAsync();
    }
}
