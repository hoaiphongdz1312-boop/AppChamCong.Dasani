using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NguyenHoaiPhong_1681.Data;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Enums;
using NguyenHoaiPhong_1681.Helpers;
using NguyenHoaiPhong_1681.Middleware;

namespace NguyenHoaiPhong_1681.Services;

public interface IDashboardService
{
    Task<DashboardDto> AdminAsync();
    Task<ManagerDashboardDto> ManagerAsync(ClaimsPrincipal user);
    Task<MyDashboardDto> MeAsync(ClaimsPrincipal user);
}

public class DashboardService : IDashboardService
{
    private const int Days = 7;

    private readonly AppDbContext _db;
    private readonly IAttendanceService _attendance;

    public DashboardService(AppDbContext db, IAttendanceService attendance)
    {
        _db = db;
        _attendance = attendance;
    }

    public async Task<DashboardDto> AdminAsync()
    {
        var ids = await _db.Users.AsNoTracking()
            .Where(u => u.Role == Role.Employee && u.Status == UserStatus.Active)
            .Select(u => u.Id).ToListAsync();
        return await BuildAsync(ids, new DashboardDto());
    }

    public async Task<ManagerDashboardDto> ManagerAsync(ClaimsPrincipal principal)
    {
        var actor = await Actor.FromAsync(principal, _db);
        if (actor.DepartmentId == null) throw new AppException(403, "Bạn chưa thuộc phòng ban nào.");

        var ids = await _db.Users.AsNoTracking()
            .Where(u => u.DepartmentId == actor.DepartmentId && u.Role == Role.Employee && u.Status == UserStatus.Active)
            .Select(u => u.Id).ToListAsync();
        var dto = await BuildAsync(ids, new ManagerDashboardDto());

        var pending = _db.LeaveRequests.AsNoTracking()
            .Where(l => l.Status == LeaveStatus.Pending && l.User!.DepartmentId == actor.DepartmentId && l.User.Role == Role.Employee);
        dto.PendingLeaveCount = await pending.CountAsync();
        dto.PendingLeaveRequests = await LeaveRequestService.Project(pending.OrderBy(l => l.CreatedAt)).Take(50).ToListAsync();
        return dto;
    }

    public async Task<MyDashboardDto> MeAsync(ClaimsPrincipal principal)
    {
        var userId = principal.GetUserId();
        var today = VnTime.Today;
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1); // đầu tháng sau (loại trừ)
        var startUtc = VnTime.ToUtc(monthStart.ToDateTime(TimeOnly.MinValue));
        var endUtc = VnTime.ToUtc(monthEnd.ToDateTime(TimeOnly.MinValue));

        var records = await _db.AttendanceRecords.AsNoTracking()
            .Where(r => r.UserId == userId && r.Timestamp >= startUtc && r.Timestamp < endUtc)
            .Select(r => new { r.Type, r.Timestamp, r.Status, r.ShiftAssignmentId })
            .ToListAsync();

        // Giờ làm = (lần check-out cuối − lần check-in đầu) của từng ca có đủ cả hai.
        double hours = 0;
        foreach (var g in records.Where(r => r.ShiftAssignmentId != null).GroupBy(r => r.ShiftAssignmentId))
        {
            var ins = g.Where(r => r.Type == AttendanceType.CheckIn).Select(r => r.Timestamp).ToList();
            var outs = g.Where(r => r.Type == AttendanceType.CheckOut).Select(r => r.Timestamp).ToList();
            if (ins.Count == 0 || outs.Count == 0) continue;
            var span = outs.Max() - ins.Min();
            if (span > TimeSpan.Zero) hours += span.TotalHours;
        }

        var leaveDays = await ApprovedLeaveDatesAsync(new List<int> { userId }, monthStart, monthEnd.AddDays(-1));
        var leaveSet = leaveDays.Where(d => d.Date.DayOfWeek != DayOfWeek.Sunday).Select(d => d.Date).ToHashSet();

        return new MyDashboardDto
        {
            Month = $"{today.Year}-{today.Month:00}",
            TotalWorkHours = Math.Round(hours, 2),
            LateCount = records.Count(r => r.Type == AttendanceType.CheckIn && r.Status == AttendanceStatus.Late),
            ApprovedLeaveDays = leaveSet.Count,
            OnLeaveToday = leaveDays.Any(d => d.Date == today),
            Today = await _attendance.TodayAsync(principal)
        };
    }

    // ------------------------------------------------------------------

    private record LeaveDay(int UserId, DateOnly Date);

    /// <summary>Các ngày nghỉ phép (Leave) đã duyệt, cắt trong [from, to].</summary>
    private async Task<List<LeaveDay>> ApprovedLeaveDatesAsync(List<int> userIds, DateOnly from, DateOnly to)
    {
        var leaves = await _db.LeaveRequests.AsNoTracking()
            .Where(l => l.Status == LeaveStatus.Approved && l.Type == LeaveType.Leave &&
                        userIds.Contains(l.UserId) && l.FromDate <= to && l.ToDate >= from)
            .Select(l => new { l.UserId, l.FromDate, l.ToDate })
            .ToListAsync();
        var result = new List<LeaveDay>();
        foreach (var l in leaves)
        {
            var d = l.FromDate < from ? from : l.FromDate;
            var last = l.ToDate > to ? to : l.ToDate;
            for (; d <= last; d = d.AddDays(1)) result.Add(new LeaveDay(l.UserId, d));
        }
        return result;
    }

    private async Task<T> BuildAsync<T>(List<int> userIds, T dto) where T : DashboardDto
    {
        var now = VnTime.Now;
        var today = DateOnly.FromDateTime(now);
        var nowTime = TimeOnly.FromDateTime(now);
        var first = today.AddDays(-(Days - 1));
        var startUtc = VnTime.ToUtc(first.ToDateTime(TimeOnly.MinValue));
        var endUtc = VnTime.ToUtc(today.AddDays(1).ToDateTime(TimeOnly.MinValue));

        dto.TotalEmployees = userIds.Count;

        var assignments = await _db.ShiftAssignments.AsNoTracking()
            .Where(a => userIds.Contains(a.UserId) && a.WorkDate >= first && a.WorkDate <= today)
            .Select(a => new { a.Id, a.UserId, a.WorkDate, a.Shift!.StartTime, a.Shift.LateToleranceMinutes })
            .ToListAsync();

        var records = await _db.AttendanceRecords.AsNoTracking()
            .Where(r => userIds.Contains(r.UserId) && r.Timestamp >= startUtc && r.Timestamp < endUtc)
            .Select(r => new { r.UserId, r.Type, r.Timestamp, r.Status, r.Method, r.IsSuspicious, r.ShiftAssignmentId })
            .ToListAsync();

        var leaves = (await ApprovedLeaveDatesAsync(userIds, first, today)).Select(l => (l.UserId, l.Date)).ToHashSet();

        var checkIns = records.Where(r => r.Type == AttendanceType.CheckIn)
            .Select(r => new { r.UserId, r.Status, r.ShiftAssignmentId, Date = DateOnly.FromDateTime(VnTime.ToVietnam(r.Timestamp)) })
            .ToList();
        var checkedAssignments = checkIns.Where(c => c.ShiftAssignmentId != null).Select(c => c.ShiftAssignmentId!.Value).ToHashSet();

        for (var d = first; d <= today; d = d.AddDays(1))
        {
            var dayIns = checkIns.Where(c => c.Date == d).ToList();
            var absentUsers = assignments
                .Where(a => a.WorkDate == d && !checkedAssignments.Contains(a.Id)
                            && !leaves.Contains((a.UserId, d))
                            && (d < today || nowTime > a.StartTime.AddMinutes(a.LateToleranceMinutes)))
                .Select(a => a.UserId).Distinct().Count();
            dto.Last7Days.Add(new DayStatDto
            {
                Date = d,
                Present = dayIns.Select(c => c.UserId).Distinct().Count(),
                Late = dayIns.Where(c => c.Status == AttendanceStatus.Late).Select(c => c.UserId).Distinct().Count(),
                Absent = absentUsers
            });
        }

        var todayStat = dto.Last7Days[^1];
        dto.PresentToday = todayStat.Present;
        dto.LateToday = todayStat.Late;
        dto.AbsentToday = todayStat.Absent;
        dto.SuspiciousLast7Days = records.Count(r => r.IsSuspicious);

        var face = records.Count(r => r.Method == AttendanceMethod.Face);
        var qr = records.Count(r => r.Method == AttendanceMethod.Qr);
        var total = face + qr;
        dto.MethodRatio = new MethodRatioDto
        {
            FaceCount = face,
            QrCount = qr,
            FacePercent = total == 0 ? 0 : Math.Round(100.0 * face / total, 1),
            QrPercent = total == 0 ? 0 : Math.Round(100.0 * qr / total, 1)
        };
        return dto;
    }
}
