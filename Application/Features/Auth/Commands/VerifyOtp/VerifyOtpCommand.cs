using Application.Features.Auth.Rules;
using Application.Persistence;
using Application.Services.SmsService;
using Application.Services.Token;
using Core.Application.Pipelines.Logging;
using Core.Security.Hashing;
using Core.Security.JWT;
using Domain.Entities.Auth;
using Domain.Entities.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Features.Auth.Commands.VerifyOtp;

// Public endpoint (no ISecuredRequest): this is how a user becomes authenticated.
public class VerifyOtpCommand : IRequest<VerifyOtpResponse>, ILoggableRequest
{
    public string PhoneNumber { get; set; } = default!;
    public string Code { get; set; } = default!;

    public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, VerifyOtpResponse>
    {
        private readonly IApplicationDbContext _db;
        private readonly ITokenHelper _tokenHelper;
        private readonly AuthBusinessRules _authBusinessRules;
        private readonly OtpSettings _otpSettings;
        private readonly TokenOptions _tokenOptions;

        public VerifyOtpCommandHandler(
            IApplicationDbContext db,
            ITokenHelper tokenHelper,
            AuthBusinessRules authBusinessRules,
            IOptions<OtpSettings> otpSettings,
            IOptions<TokenOptions> tokenOptions)
        {
            _db = db;
            _tokenHelper = tokenHelper;
            _authBusinessRules = authBusinessRules;
            _otpSettings = otpSettings.Value;
            _tokenOptions = tokenOptions.Value;
        }

        public async Task<VerifyOtpResponse> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
        {
            string phone = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
            DateTime now = DateTime.UtcNow;

            PhoneOtp? otp = await _db.PhoneOtps
                .Where(o => o.PhoneNumber == phone && !o.IsUsed && o.ExpiresAt > now)
                .OrderByDescending(o => o.CreatedDate)
                .FirstOrDefaultAsync(cancellationToken);
            await _authBusinessRules.OtpShouldExistAndBeValid(otp);
            await _authBusinessRules.OtpAttemptsShouldNotBeExceeded(otp!, _otpSettings.MaxAttempts);

            // Count this attempt before verifying so brute force is throttled.
            otp!.AttemptCount++;
            await _db.SaveChangesAsync(cancellationToken);

            bool matches = HashingHelper.VerifyPasswordHash(request.Code, otp.CodeHash, otp.CodeSalt);
            await _authBusinessRules.OtpCodeShouldMatch(matches);

            otp.IsUsed = true;
            await _db.SaveChangesAsync(cancellationToken);

            User? user = await _db.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == phone, cancellationToken);
            bool isNewUser = user is null;
            if (isNewUser)
                user = await CreatePhoneUser(phone, cancellationToken);

            // Build the access token (operation claims + JWT) inline.
            List<OperationClaim> operationClaims = await _db.UserOperationClaims
                .AsNoTracking()
                .Where(p => p.UserId == user!.Id)
                .Select(p => new OperationClaim { Id = p.OperationClaimId, Name = p.OperationClaim.Name })
                .ToListAsync(cancellationToken);
            AccessToken accessToken = _tokenHelper.CreateToken(user!, operationClaims);

            // Long-lived, rotated session: store only the hash, hand the raw value to the client once.
            (string rawRefresh, string refreshHash) = RefreshTokenHelper.Generate();
            DateTime refreshExpiresAt = now.AddDays(_tokenOptions.RefreshTokenExpirationDays);
            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user!.Id,
                TokenHash = refreshHash,
                ExpiresAt = refreshExpiresAt
            });
            await _db.SaveChangesAsync(cancellationToken);

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
            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);
            return user;
        }
    }
}
