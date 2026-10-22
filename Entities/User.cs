using NguyenHoaiPhong_1681.Enums;

namespace NguyenHoaiPhong_1681.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool PhoneVerified { get; set; }
    public string? PasswordHash { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public Role Role { get; set; } = Role.Employee;
    public UserStatus Status { get; set; } = UserStatus.PendingApproval;
    public bool EmailVerified { get; set; }
    public string? GoogleSubjectId { get; set; }
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string? BoundDeviceId { get; set; }
    public int FailedOtpCount { get; set; }
    public DateTime? LockoutUntil { get; set; }
    public bool IsDeleted { get; set; }
}
