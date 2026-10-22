using System.Security.Cryptography;
using System.Text;

namespace NguyenHoaiPhong_1681.Helpers;

public static class HashHelper
{
    public static string Sha256Hex(string input) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));

    /// <summary>So sánh chuỗi hash theo thời gian không đổi.</summary>
    public static bool FixedEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
