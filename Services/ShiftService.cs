using Microsoft.EntityFrameworkCore;
using NguyenHoaiPhong_1681.Data;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Entities;
using NguyenHoaiPhong_1681.Helpers;
using NguyenHoaiPhong_1681.Middleware;

namespace NguyenHoaiPhong_1681.Services;

public interface IShiftService
{
    Task<PagedResult<ShiftDto>> ListAsync(ShiftQuery q);
    Task<ShiftDto> GetAsync(int id);
    Task<ShiftDto> CreateAsync(ShiftRequest req);
    Task<ShiftDto> UpdateAsync(int id, ShiftRequest req);
    Task DeleteAsync(int id);
}

public class ShiftService : IShiftService
{
    private readonly AppDbContext _db;

    public ShiftService(AppDbContext db) => _db = db;

    private static ShiftDto ToDto(Shift s) => new()
    {
        Id = s.Id, Name = s.Name, StartTime = s.StartTime, EndTime = s.EndTime,
        LateToleranceMinutes = s.LateToleranceMinutes, IsActive = s.IsActive
    };

    public async Task<PagedResult<ShiftDto>> ListAsync(ShiftQuery q)
    {
        var query = _db.Shifts.AsNoTracking().AsQueryable();
        if (q.IsActive != null) query = query.Where(s => s.IsActive == q.IsActive);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(x => x.Name.Contains(s));
        }
        return await query.OrderBy(s => s.StartTime).ThenBy(s => s.Name)
            .Select(s => new ShiftDto
            {
                Id = s.Id, Name = s.Name, StartTime = s.StartTime, EndTime = s.EndTime,
                LateToleranceMinutes = s.LateToleranceMinutes, IsActive = s.IsActive
            })
            .ToPagedAsync(q.Page, q.PageSize);
    }

    public async Task<ShiftDto> GetAsync(int id) => ToDto(await FindAsync(id, tracking: false));

    public async Task<ShiftDto> CreateAsync(ShiftRequest req)
    {
        Validate(req);
        var s = new Shift();
        Apply(s, req);
        _db.Shifts.Add(s);
        await _db.SaveChangesAsync();
        return ToDto(s);
    }

    public async Task<ShiftDto> UpdateAsync(int id, ShiftRequest req)
    {
        Validate(req);
        var s = await FindAsync(id);
        Apply(s, req);
        await _db.SaveChangesAsync();
        return ToDto(s);
    }

    public async Task DeleteAsync(int id)
    {
        var s = await FindAsync(id);
        var today = VnTime.Today;
        if (await _db.ShiftAssignments.AnyAsync(a => a.ShiftId == id && a.WorkDate >= today))
            throw new AppException(409, "Không thể xóa ca đang được phân công trong hôm nay hoặc tương lai");
        s.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    private static void Validate(ShiftRequest req)
    {
        if (req.StartTime >= req.EndTime)
            throw new AppException(400, "Giờ bắt đầu phải nhỏ hơn giờ kết thúc");
    }

    private static void Apply(Shift s, ShiftRequest req)
    {
        s.Name = req.Name.Trim();
        s.StartTime = req.StartTime;
        s.EndTime = req.EndTime;
        s.LateToleranceMinutes = req.LateToleranceMinutes;
        s.IsActive = req.IsActive;
    }

    private async Task<Shift> FindAsync(int id, bool tracking = true)
    {
        var q = tracking ? _db.Shifts : _db.Shifts.AsNoTracking();
        return await q.FirstOrDefaultAsync(s => s.Id == id) ?? throw new AppException(404, "Không tìm thấy ca làm việc");
    }
}
