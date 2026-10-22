using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NguyenHoaiPhong_1681.Data;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Entities;
using NguyenHoaiPhong_1681.Enums;
using NguyenHoaiPhong_1681.Helpers;
using NguyenHoaiPhong_1681.Middleware;

namespace NguyenHoaiPhong_1681.Services;

public interface IDepartmentService
{
    Task<PagedResult<DepartmentDto>> ListAsync(ClaimsPrincipal user, PageQuery q);
    Task<DepartmentDto> GetAsync(ClaimsPrincipal user, int id);
    Task<DepartmentDto> CreateAsync(DepartmentRequest req);
    Task<DepartmentDto> UpdateAsync(int id, DepartmentRequest req);
    Task DeleteAsync(int id);
}

public class DepartmentService : IDepartmentService
{
    private readonly AppDbContext _db;

    public DepartmentService(AppDbContext db) => _db = db;

    private IQueryable<DepartmentDto> Project(IQueryable<Department> q) => q.Select(d => new DepartmentDto
    {
        Id = d.Id,
        Name = d.Name,
        Description = d.Description,
        ManagerId = d.ManagerId,
        ManagerName = d.Manager != null ? d.Manager.FullName : null,
        EmployeeCount = _db.Users.Count(u => u.DepartmentId == d.Id)
    });

    public async Task<PagedResult<DepartmentDto>> ListAsync(ClaimsPrincipal principal, PageQuery q)
    {
        var actor = await Actor.FromAsync(principal, _db);
        var query = _db.Departments.AsNoTracking().AsQueryable();
        if (!actor.IsAdmin)
        {
            if (actor.DepartmentId == null) throw new AppException(403, "Bạn chưa thuộc phòng ban nào.");
            query = query.Where(d => d.Id == actor.DepartmentId);
        }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(d => d.Name.Contains(s));
        }
        return await Project(query.OrderBy(d => d.Name)).ToPagedAsync(q.Page, q.PageSize);
    }

    public async Task<DepartmentDto> GetAsync(ClaimsPrincipal principal, int id)
    {
        var actor = await Actor.FromAsync(principal, _db);
        actor.EnsureCanManage(id, "Bạn chỉ được xem phòng ban của mình.");
        return await LoadAsync(id);
    }

    public async Task<DepartmentDto> CreateAsync(DepartmentRequest req)
    {
        await ValidateAsync(req, null);
        var d = new Department { Name = req.Name.Trim(), Description = req.Description, ManagerId = req.ManagerId };
        _db.Departments.Add(d);
        await _db.SaveChangesAsync();
        return await LoadAsync(d.Id);
    }

    public async Task<DepartmentDto> UpdateAsync(int id, DepartmentRequest req)
    {
        var d = await _db.Departments.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new AppException(404, "Không tìm thấy phòng ban");
        await ValidateAsync(req, id);
        d.Name = req.Name.Trim();
        d.Description = req.Description;
        d.ManagerId = req.ManagerId;
        await _db.SaveChangesAsync();
        return await LoadAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var d = await _db.Departments.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new AppException(404, "Không tìm thấy phòng ban");
        if (await _db.Users.AnyAsync(u => u.DepartmentId == id))
            throw new AppException(409, "Không thể xóa phòng ban còn nhân viên");
        d.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    private async Task ValidateAsync(DepartmentRequest req, int? selfId)
    {
        var name = req.Name.Trim();
        if (await _db.Departments.AnyAsync(d => d.Name == name && d.Id != selfId))
            throw new AppException(409, "Tên phòng ban đã tồn tại");
        if (req.ManagerId != null)
        {
            var mgr = await _db.Users.Where(u => u.Id == req.ManagerId).Select(u => new { u.Role }).FirstOrDefaultAsync()
                      ?? throw new AppException(400, "Trưởng phòng không tồn tại");
            if (mgr.Role == Role.Employee)
                throw new AppException(400, "Trưởng phòng phải có vai trò Manager hoặc Admin");
        }
    }

    private async Task<DepartmentDto> LoadAsync(int id) =>
        await Project(_db.Departments.AsNoTracking().Where(d => d.Id == id)).FirstOrDefaultAsync()
        ?? throw new AppException(404, "Không tìm thấy phòng ban");
}
