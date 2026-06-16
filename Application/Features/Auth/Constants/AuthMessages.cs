namespace Application.Features.Auth.Constants;

public static class AuthMessages
{
    public const string SectionName = "Auth";
    
    public const string UserDontExists = "UserDontExists";
    public const string PasswordDontMatch = "PasswordDontMatch";

    public const string OtpResendTooSoon = "OtpResendTooSoon";
    public const string OtpDailyLimitExceeded = "OtpDailyLimitExceeded";
    public const string OtpInvalidOrExpired = "OtpInvalidOrExpired";
    public const string OtpTooManyAttempts = "OtpTooManyAttempts";
    public const string OtpCodeDontMatch = "OtpCodeDontMatch";
    public const string RefreshTokenInvalid = "RefreshTokenInvalid";
}