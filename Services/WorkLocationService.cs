using System.Security.Cryptography;
using System.Security.Claims;
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

public interface IWorkLocationService
{
    Task<PagedResult<WorkLocationDto>> ListAsync(ClaimsPrincipal user, WorkLocationQuery q);
    Task<WorkLocationDto> GetAsync(ClaimsPrincipal user, int id);
    Task<WorkLocationDto> CreateAsync(WorkLocationRequest req);
    Task<WorkLocationDto> UpdateAsync(int id, WorkLocationRequest req);
    Task DeleteAsync(int id);
    Task<WorkLocationDto> RegenerateKioskKeyAsync(int id);
}

public class WorkLocationService : IWorkLocationService
{
    private readonly AppDbContext _db;
    private readonly AppOptions _app;

    public WorkLocationService(AppDbContext db, IOptions<AppOptions> app)
    {
        _db = db;
        _app = app.Value;
    }

    // KioskKey chỉ đưa vào response cho Admin.
    private WorkLocationDto ToDto(WorkLocation l, bool includeKiosk) => new()
    {
        Id = l.Id, Name = l.Name, Address = l.Address, Latitude = l.Latitude, Longitude = l.Longitude,
        RadiusMeters = l.RadiusMeters, WifiBssid = l.WifiBssid, IsActive = l.IsActive,
        KioskUrl = includeKiosk
            ? $"{_app.BaseUrl.TrimEnd('/')}/kiosk.html?locationId={l.Id}&key={l.KioskKey}"
            : null
    };

    public async Task<PagedResult<WorkLocationDto>> ListAsync(ClaimsPrincipal user, WorkLocationQuery q)
    {
        var isAdmin = user.GetRole() == Role.Admin;
        var query = _db.WorkLocations.AsNoTracking().AsQueryable();
        if (q.IsActive != null) query = query.Where(l => l.IsActive == q.IsActive);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(l => l.Name.Contains(s) || (l.Address != null && l.Address.Contains(s)));
        }
        var page = await query.OrderBy(l => l.Name).ToPagedAsync(q.Page, q.PageSize);
        return new PagedResult<WorkLocationDto>
        {
            Page = page.Page, PageSize = page.PageSize, TotalCount = page.TotalCount,
            Items = page.Items.Select(l => ToDto(l, isAdmin)).ToList()
        };
    }

    public async Task<WorkLocationDto> GetAsync(ClaimsPrincipal user, int id) =>
        ToDto(await FindAsync(id, false), user.GetRole() == Role.Admin);

    public async Task<WorkLocationDto> CreateAsync(WorkLocationRequest req)
    {
        var l = new WorkLocation
        {
            KioskKey = RandomHelper.RandomKey(32),
            QrSecret = RandomNumberGenerator.GetBytes(32)
        };
        Apply(l, req);
        _db.WorkLocations.Add(l);
        await _db.SaveChangesAsync();
        return ToDto(l, true);
    }

    public async Task<WorkLocationDto> UpdateAsync(int id, WorkLocationRequest req)
    {
        var l = await FindAsync(id);
        Apply(l, req);
        await _db.SaveChangesAsync();
        return ToDto(l, true);
    }

    public async Task DeleteAsync(int id)
    {
        var l = await FindAsync(id);
        var today = VnTime.Today;
        if (await _db.ShiftAssignments.AnyAsync(a => a.WorkLocationId == id && a.WorkDate >= today))
            throw new AppException(409, "Không thể xóa địa điểm đang được phân công trong hôm nay hoặc tương lai");
        l.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<WorkLocationDto> RegenerateKioskKeyAsync(int id)
    {
        var l = await FindAsync(id);
        l.KioskKey = RandomHelper.RandomKey(32);
        await _db.SaveChangesAsync();
        return ToDto(l, true);
    }

    private static void Apply(WorkLocation l, WorkLocationRequest req)
    {
        l.Name = req.Name.Trim();
        l.Address = req.Address;
        l.Latitude = req.Latitude;
        l.Longitude = req.Longitude;
        l.RadiusMeters = req.RadiusMeters;
        l.WifiBssid = string.IsNullOrWhiteSpace(req.WifiBssid) ? null : req.WifiBssid.Trim().ToUpperInvariant();
        l.IsActive = req.IsActive;
    }

    private async Task<WorkLocation> FindAsync(int id, bool tracking = true)
    {
        var q = tracking ? _db.WorkLocations : _db.WorkLocations.AsNoTracking();
        return await q.FirstOrDefaultAsync(l => l.Id == id) ?? throw new AppException(404, "Không tìm thấy địa điểm làm việc");
    }
}
