using System.ComponentModel.DataAnnotations;

namespace NguyenHoaiPhong_1681.DTOs;

public class ShiftAssignmentDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int LateToleranceMinutes { get; set; }
    public int WorkLocationId { get; set; }
    public string WorkLocationName { get; set; } = string.Empty;
    public double LocationLatitude { get; set; }
    public double LocationLongitude { get; set; }
    public double LocationRadiusMeters { get; set; }
    public DateOnly WorkDate { get; set; }
}

public class ShiftAssignmentQuery : PageQuery
{
    public int? UserId { get; set; }
    public int? ShiftId { get; set; }
    public int? WorkLocationId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

public class ShiftAssignmentRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn nhân viên")] public int UserId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn ca")] public int ShiftId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn địa điểm")] public int WorkLocationId { get; set; }
    [Required] public DateOnly WorkDate { get; set; }
}

public class BulkShiftAssignmentRequest
{
    [Required, MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất một nhân viên"), MaxLength(200)]
    public List<int> UserIds { get; set; } = new();

    [Range(1, int.MaxValue)] public int ShiftId { get; set; }
    [Range(1, int.MaxValue)] public int WorkLocationId { get; set; }
    [Required] public DateOnly FromDate { get; set; }
    [Required] public DateOnly ToDate { get; set; }
    public bool SkipSunday { get; set; } = true;
}

public class BulkShiftAssignmentResult
{
    public int Created { get; set; }
    public int Skipped { get; set; }
}

public class MyAssignmentQuery
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}
