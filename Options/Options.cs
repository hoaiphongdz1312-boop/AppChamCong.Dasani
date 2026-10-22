namespace NguyenHoaiPhong_1681.Options;

public class AppOptions
{
    public const string Section = "App";
    public string BaseUrl { get; set; } = "http://localhost:5280";
}

public class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int AccessMinutes { get; set; } = 30;
    public int RefreshDays { get; set; } = 7;
}

public class GoogleOptions
{
    public const string Section = "Google";
    public string WebClientId { get; set; } = string.Empty;
}

public class SmtpOptions
{
    public const string Section = "Smtp";
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string AppPassword { get; set; } = string.Empty;
    public string FromName { get; set; } = "Cham cong";
}

public class TwilioOptions
{
    public const string Section = "Twilio";
    public bool Enabled { get; set; }
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
}

public class FaceOptions
{
    public const string Section = "Face";
    public double MatchThreshold { get; set; } = 0.45;
    public double GrayZone { get; set; } = 0.05;
}

public class QrOptions
{
    public const string Section = "Qr";
    public int WindowSeconds { get; set; } = 30;
}

public class AuthOptions
{
    public const string Section = "Auth";
    public bool AutoApprove { get; set; }
}

public class OtpOptions
{
    public const string Section = "Otp";
    public bool DevMode { get; set; } = true;
    public int ExpireMinutes { get; set; } = 5;
    public int ResendSeconds { get; set; } = 60;
    public int MaxSendPerSession { get; set; } = 5;
    public int MaxSmsPerPhonePerDay { get; set; } = 10;
    public int MaxAttempts { get; set; } = 5;
    public int LockMinutes { get; set; } = 15;
}

public class SeedOptions
{
    public const string Section = "Seed";
    public double LocationLat { get; set; }
    public double LocationLng { get; set; }
    public double RadiusMeters { get; set; } = 200;
}
