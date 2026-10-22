using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NguyenHoaiPhong_1681.Data;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Entities;
using NguyenHoaiPhong_1681.Enums;
using NguyenHoaiPhong_1681.Helpers;
using NguyenHoaiPhong_1681.Middleware;

namespace NguyenHoaiPhong_1681.Services;

public interface IShiftAssignmentService
{
    Task<PagedResult<ShiftAssignmentDto>> ListAsync(ClaimsPrincipal user, ShiftAssignmentQuery q);
    Task<ShiftAssignmentDto> GetAsync(ClaimsPrincipal user, int id);
    Task<ShiftAssignmentDto> CreateAsync(ClaimsPrincipal user, ShiftAssignmentRequest req);
    Task<ShiftAssignmentDto> UpdateAsync(ClaimsPrincipal user, int id, ShiftAssignmentRequest req);
    Task DeleteAsync(ClaimsPrincipal user, int id);
    Task<BulkShiftAssignmentResult> BulkAsync(ClaimsPrincipal user, BulkShiftAssignmentRequest req);
    Task<List<ShiftAssignmentDto>> MyAsync(ClaimsPrincipal user, DateOnly? from, DateOnly? to);
}

public class ShiftAssignmentService : IShiftAssignmentService
{
    private const int MaxBulkDays = 62;

    private readonly AppDbContext _db;

    public ShiftAssignmentService(AppDbContext db) => _db = db;

    private static IQueryable<ShiftAssignmentDto> Project(IQueryable<ShiftAssignment> q) => q.Select(a => new ShiftAssignmentDto
    {
        Id = a.Id,
        UserId = a.UserId,
        UserName = a.User!.FullName,
        ShiftId = a.ShiftId,
        ShiftName = a.Shift!.Name,
        StartTime = a.Shift.StartTime,
        EndTime = a.Shift.EndTime,
        LateToleranceMinutes = a.Shift.LateToleranceMinutes,
        WorkLocationId = a.WorkLocationId,
        WorkLocationName = a.WorkLocation!.Name,
        LocationLatitude = a.WorkLocation.Latitude,
        LocationLongitude = a.WorkLocation.Longitude,
        LocationRadiusMeters = a.WorkLocation.RadiusMeters,
        WorkDate = a.WorkDate
    });

    public async Task<PagedResult<ShiftAssignmentDto>> ListAsync(ClaimsPrincipal principal, ShiftAssignmentQuery q)
    {
        var actor = await Actor.FromAsync(principal, _db);
        var query = _db.ShiftAssignments.AsNoTracking().AsQueryable();
        if (actor.IsManager)
        {
            if (actor.DepartmentId == null) throw new AppException(403, "Bạn chưa thuộc phòng ban nào.");
            query = query.Where(a => a.User!.DepartmentId == actor.DepartmentId);
        }
        if (q.UserId != null) query = query.Where(a => a.UserId == q.UserId);
        if (q.ShiftId != null) query = query.Where(a => a.ShiftId == q.ShiftId);
        if (q.WorkLocationId != null) query = query.Where(a => a.WorkLocationId == q.WorkLocationId);
        if (q.From != null) query = query.Where(a => a.WorkDate >= q.From);
        if (q.To != null) query = query.Where(a => a.WorkDate <= q.To);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(a => a.User!.FullName.Contains(s) || a.User.Username.Contains(s));
        }
        return await Project(query.OrderBy(a => a.WorkDate).ThenBy(a => a.Shift!.StartTime).ThenBy(a => a.User!.FullName))
            .ToPagedAsync(q.Page, q.PageSize);
    }

    public async Task<ShiftAssignmentDto> GetAsync(ClaimsPrincipal principal, int id)
    {
        var actor = await Actor.FromAsync(principal, _db);
        var dto = await Project(_db.ShiftAssignments.AsNoTracking().Where(a => a.Id == id)).FirstOrDefaultAsync()
                  ?? throw new AppException(404, "Không tìm thấy phân ca");
        if (dto.UserId != actor.Id) await EnsureCanManageUserAsync(actor, dto.UserId);
        return dto;
    }

    public async Task<ShiftAssignmentDto> CreateAsync(ClaimsPrincipal principal, ShiftAssignmentRequest req)
    {
        var actor = await Actor.FromAsync(principal, _db);
        await ValidateAsync(actor, req.UserId, req.ShiftId, req.WorkLocationId);
        if (await _db.ShiftAssignments.AnyAsync(a => a.UserId == req.UserId && a.WorkDate == req.WorkDate && a.ShiftId == req.ShiftId))
            throw new AppException(409, "Nhân viên đã được phân ca này trong ngày");

        var a = new ShiftAssignment
        {
            UserId = req.UserId, ShiftId = req.ShiftId, WorkLocationId = req.WorkLocationId, WorkDate = req.WorkDate
        };
        _db.ShiftAssignments.Add(a);
        await _db.SaveChangesAsync();
        return await LoadAsync(a.Id);
    }

    public async Task<ShiftAssignmentDto> UpdateAsync(ClaimsPrincipal principal, int id, ShiftAssignmentRequest req)
    {
        var actor = await Actor.FromAsync(principal, _db);
        var a = await _db.ShiftAssignments.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new AppException(404, "Không tìm thấy phân ca");
        await EnsureCanManageUserAsync(actor, a.UserId);
        await ValidateAsync(actor, req.UserId, req.ShiftId, req.WorkLocationId);
        if (await _db.ShiftAssignments.AnyAsync(x => x.Id != id && x.UserId == req.UserId && x.WorkDate == req.WorkDate && x.ShiftId == req.ShiftId))
            throw new AppException(409, "Nhân viên đã được phân ca này trong ngày");
        if (a.UserId != req.UserId && await _db.AttendanceRecords.AnyAsync(r => r.ShiftAssignmentId == id))
            throw new AppException(409, "Phân ca đã có dữ liệu chấm công, không thể đổi nhân viên");

        a.UserId = req.UserId;
        a.ShiftId = req.ShiftId;
        a.WorkLocationId = req.WorkLocationId;
        a.WorkDate = req.WorkDate;
        await _db.SaveChangesAsync();
        return await LoadAsync(id);
    }

    public async Task DeleteAsync(ClaimsPrincipal principal, int id)
    {
        var actor = await Actor.FromAsync(principal, _db);
        var a = await _db.ShiftAssignments.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new AppException(404, "Không tìm thấy phân ca");
        await EnsureCanManageUserAsync(actor, a.UserId);
        if (await _db.AttendanceRecords.AnyAsync(r => r.ShiftAssignmentId == id))
            throw new AppException(409, "Phân ca đã có dữ liệu chấm công, không thể xóa");
        _db.ShiftAssignments.Remove(a);
        await _db.SaveChangesAsync();
    }

    public async Task<BulkShiftAssignmentResult> BulkAsync(ClaimsPrincipal principal, BulkShiftAssignmentRequest req)
    {
        if (req.FromDate > req.ToDate) throw new AppException(400, "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc");
        if (req.ToDate.DayNumber - req.FromDate.DayNumber + 1 > MaxBulkDays)
            throw new AppException(400, $"Chỉ phân ca tối đa {MaxBulkDays} ngày mỗi lần");

        var actor = await Actor.FromAsync(principal, _db);
        var userIds = req.UserIds.Distinct().ToList();
        foreach (var uid in userIds) await ValidateAsync(actor, uid, req.ShiftId, req.WorkLocationId);

        var existing = (await _db.ShiftAssignments
                .Where(a => userIds.Contains(a.UserId) && a.ShiftId == req.ShiftId && a.WorkDate >= req.FromDate && a.WorkDate <= req.ToDate)
                .Select(a => new { a.UserId, a.WorkDate }).ToListAsync())
            .Select(a => (a.UserId, a.WorkDate)).ToHashSet();

        var result = new BulkShiftAssignmentResult();
        for (var d = req.FromDate; d <= req.ToDate; d = d.AddDays(1))
        {
            if (req.SkipSunday && d.DayOfWeek == DayOfWeek.Sunday) continue;
            foreach (var uid in userIds)
            {
                if (existing.Contains((uid, d))) { result.Skipped++; continue; }
                _db.ShiftAssignments.Add(new ShiftAssignment
                {
                    UserId = uid, ShiftId = req.ShiftId, WorkLocationId = req.WorkLocationId, WorkDate = d
                });
                result.Created++;
            }
        }
        await _db.SaveChangesAsync();
        return result;
    }

    public async Task<List<ShiftAssignmentDto>> MyAsync(ClaimsPrincipal principal, DateOnly? from, DateOnly? to)
    {
        var uid = principal.GetUserId();
        var f = from ?? VnTime.Today;
        var t = to ?? f.AddDays(30);
        if (t < f) throw new AppException(400, "Khoảng ngày không hợp lệ");
        if (t.DayNumber - f.DayNumber > 92) throw new AppException(400, "Chỉ xem tối đa 92 ngày mỗi lần");
        return await Project(_db.ShiftAssignments.AsNoTracking()
                .Where(a => a.UserId == uid && a.WorkDate >= f && a.WorkDate <= t)
                .OrderBy(a => a.WorkDate).ThenBy(a => a.Shift!.StartTime))
            .ToListAsync();
    }

    private async Task EnsureCanManageUserAsync(Actor actor, int userId)
    {
        if (actor.IsAdmin) return;
        var dept = await _db.Users.Where(u => u.Id == userId).Select(u => u.DepartmentId).FirstOrDefaultAsync();
        actor.EnsureCanManage(dept, "Bạn chỉ được phân ca cho nhân viên cùng phòng ban.");
    }

    private async Task ValidateAsync(Actor actor, int userId, int shiftId, int locationId)
    {
        var target = await _db.Users.Where(u => u.Id == userId).Select(u => new { u.DepartmentId, u.Status }).FirstOrDefaultAsync()
                     ?? throw new AppException(400, "Nhân viên không tồn tại");
        actor.EnsureCanManage(target.DepartmentId, "Bạn chỉ được phân ca cho nhân viên cùng phòng ban.");
        if (target.Status != UserStatus.Active) throw new AppException(400, "Nhân viên chưa được kích hoạt");
        if (!await _db.Shifts.AnyAsync(s => s.Id == shiftId && s.IsActive)) throw new AppException(400, "Ca làm việc không tồn tại hoặc đã ngừng");
        if (!await _db.WorkLocations.AnyAsync(l => l.Id == locationId && l.IsActive)) throw new AppException(400, "Địa điểm không tồn tại hoặc đã ngừng");
    }

    private async Task<ShiftAssignmentDto> LoadAsync(int id) =>
        await Project(_db.ShiftAssignments.AsNoTracking().Where(a => a.Id == id)).FirstAsync();
}
