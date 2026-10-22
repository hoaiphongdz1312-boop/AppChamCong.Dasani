namespace NguyenHoaiPhong_1681.Helpers;

public static class PhoneHelper
{
    /// <summary>Chuẩn hóa 0xxxxxxxxx / 84xxxxxxxxx / +84xxxxxxxxx thành +84xxxxxxxxx.</summary>
    public static string Normalize(string phone)
    {
        var p = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());
        if (p.StartsWith('+')) return p;
        if (p.StartsWith('0')) return "+84" + p[1..];
        return "+" + p;
    }

    public static bool IsValidE164(string phone) =>
        phone.Length is >= 9 and <= 16 && phone[0] == '+' && phone.Skip(1).All(char.IsDigit);

    /// <summary>+84912345678 → +84 9** *** 678</summary>
    public static string Mask(string? phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 8) return string.Empty;
        var digits = phone[1..];
        return $"+{digits[..2]} {digits[2]}** *** {digits[^3..]}";
    }
}
