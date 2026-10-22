using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NguyenHoaiPhong_1681.Data;
using NguyenHoaiPhong_1681.Enums;
using NguyenHoaiPhong_1681.Middleware;

namespace NguyenHoaiPhong_1681.Helpers;

/// <summary>
/// Người đang thao tác. Id và Role lấy từ claims; DepartmentId đọc lại từ DB
/// để việc đổi phòng ban có hiệu lực ngay, không phải chờ token hết hạn.
/// </summary>
public record Actor(int Id, Role Role, int? DepartmentId)
{
    public bool IsAdmin => Role == Role.Admin;
    public bool IsManager => Role == Role.Manager;

    public static async Task<Actor> FromAsync(ClaimsPrincipal principal, AppDbContext db)
    {
        var id = principal.GetUserId();
        var user = await db.Users.AsNoTracking().Where(u => u.Id == id)
            .Select(u => new { u.Role, u.DepartmentId, u.Status })
            .FirstOrDefaultAsync() ?? throw new AppException(401, "Phiên đăng nhập không hợp lệ.");
        if (user.Status != UserStatus.Active) throw new AppException(403, "Tài khoản không còn hoạt động.");
        return new Actor(id, user.Role, user.DepartmentId);
    }

    /// <summary>Manager chỉ được thao tác user cùng phòng ban; Admin thì không giới hạn.</summary>
    public void EnsureCanManage(int? targetDepartmentId, string message = "Bạn chỉ được thao tác với nhân viên cùng phòng ban.")
    {
        if (IsAdmin) return;
        if (IsManager && DepartmentId != null && DepartmentId == targetDepartmentId) return;
        throw new AppException(403, message);
    }
}
