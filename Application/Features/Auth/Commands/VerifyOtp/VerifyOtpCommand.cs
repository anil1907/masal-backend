using Application.Features.Auth.Rules;
using Application.Services.AuthService;
using Application.Services.Repositories;
using Application.Services.SmsService;
using Application.Services.Token;
using Core.Application.Pipelines.Logging;
using Core.Security.Hashing;
using Core.Security.JWT;
using Domain.Entities.Auth;
using Domain.Entities.Users;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Auth.Commands.VerifyOtp;

// Public endpoint (no ISecuredRequest): this is how a user becomes authenticated.
public class VerifyOtpCommand : IRequest<VerifyOtpResponse>, ILoggableRequest
{
    public string PhoneNumber { get; set; } = default!;
    public string Code { get; set; } = default!;

    public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, VerifyOtpResponse>
    {
        private readonly IPhoneOtpRepository _phoneOtpRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IAuthService _authService;
        private readonly AuthBusinessRules _authBusinessRules;
        private readonly OtpSettings _otpSettings;
        private readonly TokenOptions _tokenOptions;

        public VerifyOtpCommandHandler(
            IPhoneOtpRepository phoneOtpRepository,
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IAuthService authService,
            AuthBusinessRules authBusinessRules,
            IOptions<OtpSettings> otpSettings,
            IOptions<TokenOptions> tokenOptions)
        {
            _phoneOtpRepository = phoneOtpRepository;
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _authService = authService;
            _authBusinessRules = authBusinessRules;
            _otpSettings = otpSettings.Value;
            _tokenOptions = tokenOptions.Value;
        }

        public async Task<VerifyOtpResponse> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
        {
            string phone = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
            DateTime now = DateTime.UtcNow;

            PhoneOtp? otp = await _phoneOtpRepository.GetLatestActiveAsync(phone, now, cancellationToken);
            await _authBusinessRules.OtpShouldExistAndBeValid(otp);
            await _authBusinessRules.OtpAttemptsShouldNotBeExceeded(otp!, _otpSettings.MaxAttempts);

            // Count this attempt before verifying so brute force is throttled.
            otp!.AttemptCount++;
            await _phoneOtpRepository.UpdateAsync(otp, cancellationToken);

            bool matches = HashingHelper.VerifyPasswordHash(request.Code, otp.CodeHash, otp.CodeSalt);
            await _authBusinessRules.OtpCodeShouldMatch(matches);

            otp.IsUsed = true;
            await _phoneOtpRepository.UpdateAsync(otp, cancellationToken);

            User? user = await _userRepository.GetByPhoneNumber(phone);
            bool isNewUser = user is null;
            if (isNewUser)
                user = await CreatePhoneUser(phone, cancellationToken);

            AccessToken accessToken = await _authService.CreateAccessToken(user!);

            // Long-lived, rotated session: store only the hash, hand the raw value to the client once.
            (string rawRefresh, string refreshHash) = RefreshTokenHelper.Generate();
            DateTime refreshExpiresAt = now.AddDays(_tokenOptions.RefreshTokenExpirationDays);
            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                UserId = user!.Id,
                TokenHash = refreshHash,
                ExpiresAt = refreshExpiresAt
            }, cancellationToken);

            return new VerifyOtpResponse
            {
                AccessToken = accessToken,
                RefreshToken = rawRefresh,
                RefreshTokenExpiresAt = refreshExpiresAt,
                IsNewUser = isNewUser
            };
        }

        private async Task<User> CreatePhoneUser(string phone, CancellationToken cancellationToken)
        {
            // Phone-OTP users have no password; satisfy the non-null hash columns with
            // a throwaway secret that is never used to log in.
            HashingHelper.CreatePasswordHash(Guid.NewGuid().ToString("N"), out byte[] hash, out byte[] salt);

            User user = new()
            {
                Username = phone,
                Email = string.Empty,
                PhoneNumber = phone,
                PasswordHash = hash,
                PasswordSalt = salt
            };
            return await _userRepository.AddAsync(user, cancellationToken);
        }
    }
}
