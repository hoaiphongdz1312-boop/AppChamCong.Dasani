using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NguyenHoaiPhong_1681.Data;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Entities;
using NguyenHoaiPhong_1681.Enums;
using NguyenHoaiPhong_1681.Helpers;
using NguyenHoaiPhong_1681.Middleware;

namespace NguyenHoaiPhong_1681.Services;

public interface ILeaveRequestService
{
    Task<PagedResult<LeaveRequestDto>> ListAsync(ClaimsPrincipal user, LeaveRequestQuery q);
    Task<LeaveRequestDto> GetAsync(ClaimsPrincipal user, int id);
    Task<LeaveRequestDto> CreateAsync(ClaimsPrincipal user, LeaveRequestRequest req);
    Task<LeaveRequestDto> UpdateAsync(ClaimsPrincipal user, int id, LeaveRequestRequest req);
    Task CancelAsync(ClaimsPrincipal user, int id);
    Task<LeaveRequestDto> ReviewAsync(ClaimsPrincipal user, int id, ReviewLeaveRequest req);
}

public class LeaveRequestService : ILeaveRequestService
{
    private const int MaxDays = 60;

    private readonly AppDbContext _db;

    public LeaveRequestService(AppDbContext db) => _db = db;

    public static IQueryable<LeaveRequestDto> Project(IQueryable<LeaveRequest> q) => q.Select(l => new LeaveRequestDto
    {
        Id = l.Id,
        UserId = l.UserId,
        UserName = l.User!.FullName,
        Type = l.Type,
        FromDate = l.FromDate,
        ToDate = l.ToDate,
        Reason = l.Reason,
        Status = l.Status,
        ReviewerId = l.ReviewerId,
        ReviewerName = l.Reviewer != null ? l.Reviewer.FullName : null,
        ReviewNote = l.ReviewNote,
        ReviewedAt = l.ReviewedAt,
        CreatedAt = l.CreatedAt
    });

    public async Task<PagedResult<LeaveRequestDto>> ListAsync(ClaimsPrincipal principal, LeaveRequestQuery q)
    {
        var actor = await Actor.FromAsync(principal, _db);
        var query = _db.LeaveRequests.AsNoTracking().AsQueryable();

        if (actor.IsManager)
            query = query.Where(l => l.UserId == actor.Id || (actor.DepartmentId != null && l.User!.DepartmentId == actor.DepartmentId));
        else if (!actor.IsAdmin)
            query = query.Where(l => l.UserId == actor.Id);

        if (q.UserId != null) query = query.Where(l => l.UserId == q.UserId);
        if (q.Status != null) query = query.Where(l => l.Status == q.Status);
        if (q.Type != null) query = query.Where(l => l.Type == q.Type);
        // Lọc theo khoảng ngày giao nhau với [From, To].
        if (q.From != null) query = query.Where(l => l.ToDate >= q.From);
        if (q.To != null) query = query.Where(l => l.FromDate <= q.To);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(l => l.Reason.Contains(s) || l.User!.FullName.Contains(s));
        }
        return await Project(query.OrderByDescending(l => l.CreatedAt)).ToPagedAsync(q.Page, q.PageSize);
    }

    public async Task<LeaveRequestDto> GetAsync(ClaimsPrincipal principal, int id)
    {
        var actor = await Actor.FromAsync(principal, _db);
        var l = await _db.LeaveRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new AppException(404, "Không tìm thấy đơn");
        await EnsureCanViewAsync(actor, l.UserId);
        return await LoadAsync(id);
    }

    public async Task<LeaveRequestDto> CreateAsync(ClaimsPrincipal principal, LeaveRequestRequest req)
    {
        Validate(req);
        var l = new LeaveRequest
        {
            UserId = principal.GetUserId(),
            Type = req.Type,
            FromDate = req.FromDate,
            ToDate = req.ToDate,
            Reason = req.Reason.Trim(),
            Status = LeaveStatus.Pending
        };
        _db.LeaveRequests.Add(l);
        await _db.SaveChangesAsync();
        return await LoadAsync(l.Id);
    }

    public async Task<LeaveRequestDto> UpdateAsync(ClaimsPrincipal principal, int id, LeaveRequestRequest req)
    {
        Validate(req);
        var l = await FindOwnPendingAsync(principal, id, "sửa");
        l.Type = req.Type;
        l.FromDate = req.FromDate;
        l.ToDate = req.ToDate;
        l.Reason = req.Reason.Trim();
        await _db.SaveChangesAsync();
        return await LoadAsync(id);
    }

    public async Task CancelAsync(ClaimsPrincipal principal, int id)
    {
        var l = await FindOwnPendingAsync(principal, id, "hủy");
        l.Status = LeaveStatus.Cancelled;
        await _db.SaveChangesAsync();
    }

    public async Task<LeaveRequestDto> ReviewAsync(ClaimsPrincipal principal, int id, ReviewLeaveRequest req)
    {
        var actor = await Actor.FromAsync(principal, _db);
        var l = await _db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new AppException(404, "Không tìm thấy đơn");
        if (l.UserId == actor.Id) throw new AppException(403, "Bạn không thể tự duyệt đơn của mình");

        if (actor.IsManager)
        {
            var owner = await _db.Users.Where(u => u.Id == l.UserId).Select(u => new { u.DepartmentId, u.Role }).FirstOrDefaultAsync()
                        ?? throw new AppException(404, "Không tìm thấy người gửi đơn");
            actor.EnsureCanManage(owner.DepartmentId, "Bạn chỉ được duyệt đơn của nhân viên cùng phòng ban.");
            if (owner.Role != Role.Employee) throw new AppException(403, "Bạn chỉ được duyệt đơn của nhân viên.");
        }
        if (l.Status != LeaveStatus.Pending) throw new AppException(409, "Đơn đã được xử lý hoặc đã hủy");

        l.Status = req.Approve ? LeaveStatus.Approved : LeaveStatus.Rejected;
        l.ReviewerId = actor.Id;
        l.ReviewNote = req.Note;
        l.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return await LoadAsync(id);
    }

    private static void Validate(LeaveRequestRequest req)
    {
        if (req.FromDate > req.ToDate) throw new AppException(400, "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc");
        if (req.ToDate.DayNumber - req.FromDate.DayNumber + 1 > MaxDays)
            throw new AppException(400, $"Đơn tối đa {MaxDays} ngày");
    }

    private async Task<LeaveRequest> FindOwnPendingAsync(ClaimsPrincipal principal, int id, string verb)
    {
        var uid = principal.GetUserId();
        var l = await _db.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new AppException(404, "Không tìm thấy đơn");
        if (l.UserId != uid) throw new AppException(403, "Bạn chỉ được thao tác với đơn của mình");
        if (l.Status != LeaveStatus.Pending) throw new AppException(409, $"Chỉ {verb} được đơn đang chờ duyệt");
        return l;
    }

    private async Task EnsureCanViewAsync(Actor actor, int ownerId)
    {
        if (actor.IsAdmin || ownerId == actor.Id) return;
        if (actor.IsManager)
        {
            var dept = await _db.Users.Where(u => u.Id == ownerId).Select(u => u.DepartmentId).FirstOrDefaultAsync();
            actor.EnsureCanManage(dept, "Bạn chỉ được xem đơn của nhân viên cùng phòng ban.");
            return;
        }
        throw new AppException(403, "Bạn không có quyền xem đơn này");
    }

    private async Task<LeaveRequestDto> LoadAsync(int id) =>
        await Project(_db.LeaveRequests.AsNoTracking().Where(l => l.Id == id)).FirstAsync();
}
