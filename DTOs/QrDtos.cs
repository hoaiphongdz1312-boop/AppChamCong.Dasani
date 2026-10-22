namespace NguyenHoaiPhong_1681.DTOs;

public class QrResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int SecondsLeft { get; set; }

    /// <summary>Độ dài một khung thời gian (giây), để trang kiosk vẽ thanh đếm ngược.</summary>
    public int WindowSeconds { get; set; }

    public string LocationName { get; set; } = string.Empty;

    /// <summary>Ảnh PNG của mã QR, mã hóa base64 (không kèm tiền tố data:).</summary>
    public string QrPngBase64 { get; set; } = string.Empty;
}
