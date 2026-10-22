using System.Security.Cryptography;

namespace NguyenHoaiPhong_1681.Helpers;

public static class RandomHelper
{
    private const string KeyAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    public static string RandomKey(int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = KeyAlphabet[RandomNumberGenerator.GetInt32(KeyAlphabet.Length)];
        return new string(chars);
    }
}
