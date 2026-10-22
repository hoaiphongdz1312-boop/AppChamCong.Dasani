using NguyenHoaiPhong_1681.Enums;

namespace NguyenHoaiPhong_1681.Entities;

public class LeaveRequest : BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }
    public LeaveType Type { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public int? ReviewerId { get; set; }
    public User? Reviewer { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
