using System.ComponentModel.DataAnnotations;

namespace NguyenHoaiPhong_1681.DTOs;

public class ShareLocationRequest
{
    [Range(-90, 90, ErrorMessage = "Vĩ độ không hợp lệ")] public double Latitude { get; set; }
    [Range(-180, 180, ErrorMessage = "Kinh độ không hợp lệ")] public double Longitude { get; set; }
    [Range(0, 100000, ErrorMessage = "Độ chính xác không hợp lệ")] public double Accuracy { get; set; }

    /// <summary>false = tắt chia sẻ vị trí.</summary>
    public bool IsSharing { get; set; }
}

public class LiveLocationDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Accuracy { get; set; }
    public DateTime Timestamp { get; set; }
}

public class LocationPointDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Accuracy { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsSharing { get; set; }
}

public class LocationHistoryQuery
{
    /// <summary>Ngày (giờ Việt Nam), mặc định hôm nay.</summary>
    public DateOnly? Date { get; set; }
}
