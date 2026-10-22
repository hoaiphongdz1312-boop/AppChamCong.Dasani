using System.ComponentModel.DataAnnotations;
using NguyenHoaiPhong_1681.Enums;

namespace NguyenHoaiPhong_1681.DTOs;

/// <summary>Dữ liệu multipart cho check-in và check-out.</summary>
public class CheckRequest
{
    [Range(-90, 90, ErrorMessage = "Vĩ độ không hợp lệ")] public double Latitude { get; set; }
    [Range(-180, 180, ErrorMessage = "Kinh độ không hợp lệ")] public double Longitude { get; set; }
    public double Accuracy { get; set; }
    public bool IsMockLocation { get; set; }

    [Required(ErrorMessage = "Thiếu deviceId"), StringLength(128)]
    public string DeviceId { get; set; } = string.Empty;

    [StringLength(32)] public string? WifiBssid { get; set; }

    /// <summary>Face hoặc Qr.</summary>
    [Required] public AttendanceMethod Method { get; set; }

    // method = Face
    public IFormFile? Image { get; set; }
    public Guid? ChallengeToken { get; set; }
    public bool LivenessPassed { get; set; }

    // method = Qr
    [StringLength(128)] public string? QrToken { get; set; }
}

public class CheckResponse
{
    public int RecordId { get; set; }
    public AttendanceType Type { get; set; }
    public AttendanceStatus Status { get; set; }
    public int TrustScore { get; set; }
    public bool IsSuspicious { get; set; }
    public double DistanceMeters { get; set; }
    public AttendanceMethod Method { get; set; }
    public DateTime Timestamp { get; set; }

    /// <summary>Lý do bị trừ điểm tin cậy (tiếng Việt).</summary>
    public List<string> Reasons { get; set; } = new();
}

public class TodayShiftDto
{
    public int AssignmentId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int LateToleranceMinutes { get; set; }
    public int WorkLocationId { get; set; }
    public string WorkLocationName { get; set; } = string.Empty;
    public double LocationLatitude { get; set; }
    public double LocationLongitude { get; set; }
    public double LocationRadiusMeters { get; set; }
    public AttendanceRecordDto? CheckIn { get; set; }
    public AttendanceRecordDto? CheckOut { get; set; }
    public bool CanCheckIn { get; set; }
    public bool CanCheckOut { get; set; }
}

public class TodayDto
{
    public DateOnly Date { get; set; }
    public List<TodayShiftDto> Shifts { get; set; } = new();
}

public class MyAttendanceQuery : PageQuery
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

public class DepartmentAttendanceQuery : PageQuery
{
    /// <summary>Ngày (giờ Việt Nam), mặc định hôm nay.</summary>
    public DateOnly? Date { get; set; }
}

public class AttendanceQuery : PageQuery
{
    public int? UserId { get; set; }
    public int? DepartmentId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public AttendanceMethod? Method { get; set; }
    public bool? IsSuspicious { get; set; }
}

public class SuspiciousQuery : PageQuery
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}
