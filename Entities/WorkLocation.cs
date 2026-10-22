namespace NguyenHoaiPhong_1681.Entities;

public class WorkLocation : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusMeters { get; set; } = 200;
    public string? WifiBssid { get; set; }

    /// <summary>Chuỗi ngẫu nhiên 32 ký tự để mở trang kiosk.</summary>
    public string KioskKey { get; set; } = string.Empty;

    /// <summary>Khóa HMAC 32 byte riêng của địa điểm, dùng ký QR động.</summary>
    public byte[] QrSecret { get; set; } = Array.Empty<byte>();

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
}
