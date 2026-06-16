using Application.Features.Auth.Constants;
using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exception.Types;
using Core.Localization.Abstraction;
using Core.Security.Hashing;
using Domain.Entities.Auth;
using Domain.Entities.Users;

namespace Application.Features.Auth.Rules;

public class AuthBusinessRules : BaseBusinessRules
{
    private readonly ILocalizationService _localizationService;

    public AuthBusinessRules(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public async Task UserShouldBeExistsWhenSelected(User? user)
    {
        if (user == null)
            await throwBusinessException(AuthMessages.UserDontExists);
    }

    public async Task UserPasswordShouldBeMatch(User user, string password)
    {
        if (!HashingHelper.VerifyPasswordHash(password, user!.PasswordHash, user.PasswordSalt))
            await throwBusinessException(AuthMessages.PasswordDontMatch);
    }

    public async Task OtpResendCooldownShouldBePassed(PhoneOtp? lastActiveOtp, int cooldownSeconds, DateTime nowUtc)
    {
        if (lastActiveOtp is not null &&
            lastActiveOtp.CreatedDate.AddSeconds(cooldownSeconds) > nowUtc)
            await throwBusinessException(AuthMessages.OtpResendTooSoon);
    }

    public async Task OtpDailyLimitShouldNotBeExceeded(int sentInLastDay, int dailyLimit)
    {
        if (sentInLastDay >= dailyLimit)
            await throwBusinessException(AuthMessages.OtpDailyLimitExceeded);
    }

    public async Task OtpShouldExistAndBeValid(PhoneOtp? otp)
    {
        // GetLatestActiveAsync already filters out used/expired codes, so a null here
        // means "no valid code" - same user-facing message for not-found and expired.
        if (otp is null)
            await throwBusinessException(AuthMessages.OtpInvalidOrExpired);
    }

    public async Task OtpAttemptsShouldNotBeExceeded(PhoneOtp otp, int maxAttempts)
    {
        if (otp.AttemptCount >= maxAttempts)
            await throwBusinessException(AuthMessages.OtpTooManyAttempts);
    }

    public async Task OtpCodeShouldMatch(bool matches)
    {
        if (!matches)
            await throwBusinessException(AuthMessages.OtpCodeDontMatch);
    }

    public async Task RefreshTokenShouldBeValid(RefreshToken? token)
    {
        // Null means: unknown hash, already revoked (rotation), or expired.
        if (token is null)
            await throwBusinessException(AuthMessages.RefreshTokenInvalid);
    }


    private async Task throwBusinessException(string messageKey)
    {
        string message = await _localizationService.GetLocalizedAsync(messageKey, AuthMessages.SectionName);
        throw new BusinessException(message);
    }
} 