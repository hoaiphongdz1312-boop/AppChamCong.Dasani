using System.ComponentModel.DataAnnotations;
using NguyenHoaiPhong_1681.Enums;

namespace NguyenHoaiPhong_1681.DTOs;

public class LeaveRequestDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public LeaveType Type { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveStatus Status { get; set; }
    public int? ReviewerId { get; set; }
    public string? ReviewerName { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LeaveRequestQuery : PageQuery
{
    public LeaveStatus? Status { get; set; }
    public LeaveType? Type { get; set; }
    public int? UserId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

public class LeaveRequestRequest
{
    [Required] public LeaveType Type { get; set; }
    [Required] public DateOnly FromDate { get; set; }
    [Required] public DateOnly ToDate { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do"), StringLength(1000)]
    public string Reason { get; set; } = string.Empty;
}

public class ReviewLeaveRequest
{
    public bool Approve { get; set; }

    [StringLength(500)] public string? Note { get; set; }
}

public class AttendanceRecordDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int? ShiftAssignmentId { get; set; }
    public AttendanceType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceMeters { get; set; }
    public bool IsMockLocation { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string? WifiBssid { get; set; }
    public AttendanceMethod Method { get; set; }
    public double? FaceScore { get; set; }
    public bool? LivenessPassed { get; set; }
    public int TrustScore { get; set; }
    public bool IsSuspicious { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Note { get; set; }
    public int? EditedById { get; set; }
    public string? EditReason { get; set; }
}

public class EditAttendanceRequest
{
    [Required] public AttendanceType Type { get; set; }

    /// <summary>Thời điểm ISO-8601. Nếu không kèm múi giờ thì hiểu là giờ Việt Nam.</summary>
    [Required] public DateTime Timestamp { get; set; }

    [Required] public AttendanceStatus Status { get; set; }
    [StringLength(512)] public string? Note { get; set; }

    [Required(ErrorMessage = "Bắt buộc nhập lý do chỉnh sửa"), StringLength(512, MinimumLength = 3, ErrorMessage = "Lý do chỉnh sửa tối thiểu 3 ký tự")]
    public string EditReason { get; set; } = string.Empty;
}
