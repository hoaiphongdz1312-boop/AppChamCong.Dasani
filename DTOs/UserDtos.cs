using System.ComponentModel.DataAnnotations;
using NguyenHoaiPhong_1681.Enums;

namespace NguyenHoaiPhong_1681.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool PhoneVerified { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public Role Role { get; set; }
    public UserStatus Status { get; set; }
    public bool EmailVerified { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? BoundDeviceId { get; set; }
    public bool HasFaceEnrolled { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserQuery : PageQuery
{
    public Role? Role { get; set; }
    public UserStatus? Status { get; set; }
    public int? DepartmentId { get; set; }
}

public class CreateUserRequest
{
    [Required, RegularExpression(@"^[a-zA-Z0-9_.]{3,32}$", ErrorMessage = "Tên đăng nhập gồm 3-32 ký tự chữ, số, dấu chấm hoặc gạch dưới")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ"), StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại"), StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{8,}$", ErrorMessage = "Mật khẩu tối thiểu 8 ký tự, gồm cả chữ và số")]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(128)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(512)] public string? AvatarUrl { get; set; }
    public Role Role { get; set; } = Role.Employee;
    public int? DepartmentId { get; set; }
}

public class UpdateUserRequest
{
    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ"), StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại"), StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(128)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(512)] public string? AvatarUrl { get; set; }

    /// <summary>Để trống nếu không đổi mật khẩu.</summary>
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{8,}$", ErrorMessage = "Mật khẩu tối thiểu 8 ký tự, gồm cả chữ và số")]
    [StringLength(100)]
    public string? NewPassword { get; set; }
}

public class UpdateMeRequest
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(128)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(512)] public string? AvatarUrl { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại"), StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class SetStatusRequest
{
    [Required] public UserStatus Status { get; set; }
}

public class SetRoleRequest
{
    [Required] public Role Role { get; set; }
}

public class SetDepartmentRequest
{
    /// <summary>null = bỏ khỏi phòng ban.</summary>
    public int? DepartmentId { get; set; }
}

public class OtpSendLogDto
{
    public int Id { get; set; }
    public OtpChannel Channel { get; set; }
    public string Destination { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DepartmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public int EmployeeCount { get; set; }
}

public class DepartmentRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên phòng ban"), StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [StringLength(512)] public string? Description { get; set; }
    public int? ManagerId { get; set; }
}
