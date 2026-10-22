namespace NguyenHoaiPhong_1681.Helpers;

public static class MaskHelper
{
    /// <summary>nguyenvan@gmail.com → ng*******@gmail.com</summary>
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email)) return string.Empty;
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        var name = email[..at];
        var keep = Math.Min(2, name.Length);
        return name[..keep] + new string('*', Math.Max(name.Length - keep, 1)) + email[at..];
    }
}
