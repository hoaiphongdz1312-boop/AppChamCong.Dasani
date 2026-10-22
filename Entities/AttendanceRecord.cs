using NguyenHoaiPhong_1681.Enums;

namespace NguyenHoaiPhong_1681.Entities;

public class AttendanceRecord : BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }
    public int? ShiftAssignmentId { get; set; }
    public ShiftAssignment? ShiftAssignment { get; set; }
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
    public long? QrWindow { get; set; }
    public int TrustScore { get; set; } = 100;
    public bool IsSuspicious { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Note { get; set; }
    public int? EditedById { get; set; }
    public User? EditedBy { get; set; }
    public string? EditReason { get; set; }
}
