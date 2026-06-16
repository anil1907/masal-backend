namespace Application.Services.SmsService;

/// <summary>
/// OTP behaviour, bound from the "OtpSettings" section of appsettings.
/// Defaults are sane for an MVP; tune without code changes.
/// </summary>
public class OtpSettings
{
    public int CodeLength { get; set; } = 6;
    public int ExpireMinutes { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 60;
    public int MaxAttempts { get; set; } = 5;
    public int DailyLimit { get; set; } = 10;
}
