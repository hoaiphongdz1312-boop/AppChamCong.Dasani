using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NguyenHoaiPhong_1681.Data;
using NguyenHoaiPhong_1681.DTOs;
using NguyenHoaiPhong_1681.Entities;
using NguyenHoaiPhong_1681.Enums;
using NguyenHoaiPhong_1681.Helpers;
using NguyenHoaiPhong_1681.Middleware;

namespace NguyenHoaiPhong_1681.Services;

public interface IUserService
{
    Task<PagedResult<UserDto>> ListAsync(ClaimsPrincipal user, UserQuery q);
    Task<UserDto> GetAsync(ClaimsPrincipal user, int id);
    Task<UserDto> CreateAsync(CreateUserRequest req);
    Task<UserDto> UpdateAsync(int id, UpdateUserRequest req);
    Task DeleteAsync(ClaimsPrincipal user, int id);
    Task<UserDto> ApproveAsync(int id);
    Task<UserDto> SetStatusAsync(ClaimsPrincipal user, int id, UserStatus status);
    Task<UserDto> SetRoleAsync(ClaimsPrincipal user, int id, Role role);
    Task<UserDto> SetDepartmentAsync(int id, int? departmentId);
    Task<UserDto> ResetDeviceAsync(int id);
    Task<PagedResult<OtpSendLogDto>> OtpLogsAsync(int id, PageQuery q);
    Task<UserDto> GetMeAsync(ClaimsPrincipal user);
    Task<UserDto> UpdateMeAsync(ClaimsPrincipal user, UpdateMeRequest req);
}

public class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db) => _db = db;

    private IQueryable<UserDto> Project(IQueryable<User> q) => q.Select(u => new UserDto
    {
        Id = u.Id,
        Username = u.Username,
        Email = u.Email,
        PhoneNumber = u.PhoneNumber,
        PhoneVerified = u.PhoneVerified,
        FullName = u.FullName,
        AvatarUrl = u.AvatarUrl,
        Role = u.Role,
        Status = u.Status,
        EmailVerified = u.EmailVerified,
        DepartmentId = u.DepartmentId,
        DepartmentName = u.Department != null ? u.Department.Name : null,
        BoundDeviceId = u.BoundDeviceId,
        HasFaceEnrolled = _db.FaceEmbeddings.Any(f => f.UserId == u.Id),
        CreatedAt = u.CreatedAt
    });

    public async Task<PagedResult<UserDto>> ListAsync(ClaimsPrincipal principal, UserQuery q)
    {
        var actor = await Actor.FromAsync(principal, _db);
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (actor.IsManager)
        {
            if (actor.DepartmentId == null) throw new AppException(403, "Bạn chưa thuộc phòng ban nào.");
            query = query.Where(u => u.DepartmentId == actor.DepartmentId);
        }
        else if (q.DepartmentId != null)
        {
            query = query.Where(u => u.DepartmentId == q.DepartmentId);
        }

        if (q.Role != null) query = query.Where(u => u.Role == q.Role);
        if (q.Status != null) query = query.Where(u => u.Status == q.Status);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(u => u.Username.Contains(s) || u.FullName.Contains(s) || u.Email.Contains(s));
        }
        return await Project(query.OrderBy(u => u.FullName)).ToPagedAsync(q.Page, q.PageSize);
    }

    public async Task<UserDto> GetAsync(ClaimsPrincipal principal, int id)
    {
        var actor = await Actor.FromAsync(principal, _db);
        var dto = await Project(_db.Users.AsNoTracking().Where(u => u.Id == id)).FirstOrDefaultAsync()
                  ?? throw new AppException(404, "Không tìm thấy người dùng");
        if (actor.Id != id) actor.EnsureCanManage(dto.DepartmentId);
        return dto;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest req)
    {
        var username = req.Username.Trim();
        var email = req.Email.Trim().ToLowerInvariant();
        var phone = PhoneHelper.Normalize(req.PhoneNumber);
        if (!PhoneHelper.IsValidE164(phone)) throw new AppException(400, "Số điện thoại không hợp lệ");
        if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == username))
            throw new AppException(409, "Tên đăng nhập đã tồn tại");
        if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email))
            throw new AppException(409, "Email đã được sử dụng");
        await EnsureDepartmentExistsAsync(req.DepartmentId);

        var user = new User
        {
            Username = username,
            Email = email,
            PhoneNumber = phone,
            FullName = req.FullName.Trim(),
            AvatarUrl = req.AvatarUrl,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = req.Role,
            Status = UserStatus.Active,
            EmailVerified = true,
            DepartmentId = req.DepartmentId
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return await LoadAsync(user.Id);
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest req)
    {
        var user = await FindAsync(id);
        var email = req.Email.Trim().ToLowerInvariant();
        var phone = PhoneHelper.Normalize(req.PhoneNumber);
        if (!PhoneHelper.IsValidE164(phone)) throw new AppException(400, "Số điện thoại không hợp lệ");
        if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email && u.Id != id))
            throw new AppException(409, "Email đã được sử dụng");

        if (phone != user.PhoneNumber) user.PhoneVerified = false;
        user.Email = email;
        user.PhoneNumber = phone;
        user.FullName = req.FullName.Trim();
        user.AvatarUrl = req.AvatarUrl;
        if (!string.IsNullOrEmpty(req.NewPassword))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await _db.SaveChangesAsync();
        return await LoadAsync(id);
    }

    public async Task DeleteAsync(ClaimsPrincipal principal, int id)
    {
        if (principal.GetUserId() == id) throw new AppException(400, "Bạn không thể tự xóa tài khoản của mình");
        var user = await FindAsync(id);
        user.IsDeleted = true;
        await RevokeTokensAsync(id);

        // Gỡ khỏi vai trò trưởng phòng nếu có.
        var depts = await _db.Departments.Where(d => d.ManagerId == id).ToListAsync();
        foreach (var d in depts) d.ManagerId = null;
        await _db.SaveChangesAsync();
    }

    public async Task<UserDto> ApproveAsync(int id)
    {
        var user = await FindAsync(id);
        if (user.Status != UserStatus.PendingApproval)
            throw new AppException(400, "Chỉ duyệt được tài khoản đang chờ duyệt");
        user.Status = UserStatus.Active;
        await _db.SaveChangesAsync();
        return await LoadAsync(id);
    }

    public async Task<UserDto> SetStatusAsync(ClaimsPrincipal principal, int id, UserStatus status)
    {
        if (status == UserStatus.PendingApproval)
            throw new AppException(400, "Chỉ có thể đặt trạng thái Active hoặc Locked");
        if (principal.GetUserId() == id && status == UserStatus.Locked)
            throw new AppException(400, "Bạn không thể tự khóa tài khoản của mình");

        var user = await FindAsync(id);
        user.Status = status;
        if (status == UserStatus.Active)
        {
            user.FailedOtpCount = 0;
            user.LockoutUntil = null;
        }
        else
        {
            await RevokeTokensAsync(id);
        }
        await _db.SaveChangesAsync();
        return await LoadAsync(id);
    }

    public async Task<UserDto> SetRoleAsync(ClaimsPrincipal principal, int id, Role role)
    {
        if (principal.GetUserId() == id) throw new AppException(400, "Bạn không thể tự đổi vai trò của mình");
        var user = await FindAsync(id);
        user.Role = role;
        await _db.SaveChangesAsync();
        return await LoadAsync(id);
    }

    public async Task<UserDto> SetDepartmentAsync(int id, int? departmentId)
    {
        await EnsureDepartmentExistsAsync(departmentId);
        var user = await FindAsync(id);
        user.DepartmentId = departmentId;
        await _db.SaveChangesAsync();
        return await LoadAsync(id);
    }

    public async Task<UserDto> ResetDeviceAsync(int id)
    {
        var user = await FindAsync(id);
        user.BoundDeviceId = null;
        await _db.SaveChangesAsync();
        return await LoadAsync(id);
    }

    public async Task<PagedResult<OtpSendLogDto>> OtpLogsAsync(int id, PageQuery q)
    {
        await FindAsync(id);
        return await _db.OtpSendLogs.AsNoTracking()
            .Where(l => l.UserId == id)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new OtpSendLogDto
            {
                Id = l.Id, Channel = l.Channel, Destination = l.Destination,
                Success = l.Success, Error = l.Error, CreatedAt = l.CreatedAt
            })
            .ToPagedAsync(q.Page, q.PageSize);
    }

    public Task<UserDto> GetMeAsync(ClaimsPrincipal principal) => LoadAsync(principal.GetUserId());

    public async Task<UserDto> UpdateMeAsync(ClaimsPrincipal principal, UpdateMeRequest req)
    {
        var id = principal.GetUserId();
        var user = await FindAsync(id);
        var phone = PhoneHelper.Normalize(req.PhoneNumber);
        if (!PhoneHelper.IsValidE164(phone)) throw new AppException(400, "Số điện thoại không hợp lệ");

        if (phone != user.PhoneNumber) user.PhoneVerified = false;
        user.PhoneNumber = phone;
        user.FullName = req.FullName.Trim();
        user.AvatarUrl = req.AvatarUrl;
        await _db.SaveChangesAsync();
        return await LoadAsync(id);
    }

    private async Task<User> FindAsync(int id) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Id == id) ?? throw new AppException(404, "Không tìm thấy người dùng");

    private async Task<UserDto> LoadAsync(int id) =>
        await Project(_db.Users.AsNoTracking().Where(u => u.Id == id)).FirstOrDefaultAsync()
        ?? throw new AppException(404, "Không tìm thấy người dùng");

    private async Task EnsureDepartmentExistsAsync(int? departmentId)
    {
        if (departmentId != null && !await _db.Departments.AnyAsync(d => d.Id == departmentId))
            throw new AppException(400, "Phòng ban không tồn tại");
    }

    private async Task RevokeTokensAsync(int userId)
    {
        var tokens = await _db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync();
        foreach (var t in tokens) t.RevokedAt = DateTime.UtcNow;
    }
}
