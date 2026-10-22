namespace NguyenHoaiPhong_1681.Entities;

public class FaceEmbedding : BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }

    /// <summary>Vector ArcFace 512 chiều đã L2 normalize. Không lưu ảnh khuôn mặt.</summary>
    public float[] Vector { get; set; } = new float[512];

    public int SampleCount { get; set; }
}

public class FaceChallenge : BaseEntity
{
    public int? UserId { get; set; }
    public User? User { get; set; }
    public Guid Token { get; set; }

    /// <summary>Ví dụ "blink,turn_left".</summary>
    public string Actions { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; }
}

public class LocationShare : BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Accuracy { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsSharing { get; set; }
}
