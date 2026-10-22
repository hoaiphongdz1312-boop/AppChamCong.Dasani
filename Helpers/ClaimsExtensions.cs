using System.Security.Claims;
using NguyenHoaiPhong_1681.Enums;
using NguyenHoaiPhong_1681.Middleware;

namespace NguyenHoaiPhong_1681.Helpers;

public static class ClaimsExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var v = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(v, out var id) ? id : throw new AppException(401, "Phiên đăng nhập không hợp lệ.");
    }

    public static Role GetRole(this ClaimsPrincipal user)
    {
        var v = user.FindFirstValue("role") ?? user.FindFirstValue(ClaimTypes.Role);
        return Enum.TryParse<Role>(v, out var r) ? r : Role.Employee;
    }

    public static int? GetDepartmentId(this ClaimsPrincipal user)
    {
        var v = user.FindFirstValue("departmentId");
        return int.TryParse(v, out var id) ? id : null;
    }
}
