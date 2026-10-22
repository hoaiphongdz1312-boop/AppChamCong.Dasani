namespace NguyenHoaiPhong_1681.Helpers;

public static class VnTime
{
    private static readonly TimeZoneInfo Zone = FindZone();

    private static TimeZoneInfo FindZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
    }

    public static DateTime ToVietnam(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    public static DateTime ToUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone);

    public static DateTime Now => ToVietnam(DateTime.UtcNow);

    public static DateOnly Today => DateOnly.FromDateTime(Now);
}
