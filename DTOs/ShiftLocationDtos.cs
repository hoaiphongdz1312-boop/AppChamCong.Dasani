using System.ComponentModel.DataAnnotations;

namespace NguyenHoaiPhong_1681.DTOs;

public class ShiftDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int LateToleranceMinutes { get; set; }
    public bool IsActive { get; set; }
}

public class ShiftQuery : PageQuery
{
    public bool? IsActive { get; set; }
}

public class ShiftRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên ca"), StringLength(64)]
    public string Name { get; set; } = string.Empty;

    [Required] public TimeOnly StartTime { get; set; }
    [Required] public TimeOnly EndTime { get; set; }

    [Range(0, 120, ErrorMessage = "Số phút trễ cho phép từ 0 đến 120")]
    public int LateToleranceMinutes { get; set; }

    public bool IsActive { get; set; } = true;
}

public class WorkLocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusMeters { get; set; }
    public string? WifiBssid { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Chỉ trả cho Admin.</summary>
    public string? KioskUrl { get; set; }
}

public class WorkLocationQuery : PageQuery
{
    public bool? IsActive { get; set; }
}

public class WorkLocationRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên địa điểm"), StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [StringLength(256)] public string? Address { get; set; }

    [Range(-90, 90, ErrorMessage = "Vĩ độ không hợp lệ")]
    public double Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Kinh độ không hợp lệ")]
    public double Longitude { get; set; }

    [Range(20, 5000, ErrorMessage = "Bán kính từ 20 đến 5000 mét")]
    public double RadiusMeters { get; set; } = 200;

    [StringLength(32)] public string? WifiBssid { get; set; }
    public bool IsActive { get; set; } = true;
}
