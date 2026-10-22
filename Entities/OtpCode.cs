using NguyenHoaiPhong_1681.Enums;

namespace NguyenHoaiPhong_1681.Entities;

public class OtpCode : BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }
    public Guid SessionId { get; set; }
    public OtpPurpose Purpose { get; set; }
    public OtpChannel? Channel { get; set; }
    public string? CodeHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public int SendCount { get; set; }
    public DateTime? LastSentAt { get; set; }
    public bool Used { get; set; }
}

/// <summary>Lịch sử gửi OTP cho admin xem. Không lưu mã.</summary>
public class OtpSendLog : BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }
    public OtpChannel Channel { get; set; }
    public string Destination { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string DeviceId { get; set; } = string.Empty;
}
