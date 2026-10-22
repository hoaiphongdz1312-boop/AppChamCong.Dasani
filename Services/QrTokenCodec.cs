using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace NguyenHoaiPhong_1681.Services;

/// <summary>
/// Mã hóa/giải mã token QR: Base64Url( locationId (4 byte) | window (8 byte) | HMAC-SHA256 cắt 16 byte ).
/// HMAC tính trên chuỗi "{locationId}|{window}" bằng QrSecret riêng của địa điểm.
/// </summary>
public static class QrTokenCodec
{
    private const int MacLength = 16;
    private const int TokenLength = 4 + 8 + MacLength;

    public static string Create(int locationId, long window, byte[] secret)
    {
        var buffer = new byte[TokenLength];
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(0, 4), locationId);
        BinaryPrimitives.WriteInt64BigEndian(buffer.AsSpan(4, 8), window);
        ComputeMac(locationId, window, secret).CopyTo(buffer.AsSpan(12));
        return ToBase64Url(buffer);
    }

    /// <summary>Giải mã phần locationId và window (chưa kiểm tra chữ ký). Sai định dạng → false.</summary>
    public static bool TryParse(string token, out int locationId, out long window, out byte[] mac)
    {
        locationId = 0;
        window = 0;
        mac = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(token) || token.Length > 128) return false;

        byte[] raw;
        try { raw = FromBase64Url(token.Trim()); }
        catch (FormatException) { return false; }
        if (raw.Length != TokenLength) return false;

        locationId = BinaryPrimitives.ReadInt32BigEndian(raw.AsSpan(0, 4));
        window = BinaryPrimitives.ReadInt64BigEndian(raw.AsSpan(4, 8));
        mac = raw[12..];
        return true;
    }

    /// <summary>So HMAC bằng FixedTimeEquals.</summary>
    public static bool VerifyMac(int locationId, long window, byte[] mac, byte[] secret) =>
        CryptographicOperations.FixedTimeEquals(ComputeMac(locationId, window, secret), mac);

    private static byte[] ComputeMac(int locationId, long window, byte[] secret)
    {
        var full = HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes($"{locationId}|{window}"));
        return full[..MacLength];
    }

    private static string ToBase64Url(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
        return Convert.FromBase64String(s);
    }
}
