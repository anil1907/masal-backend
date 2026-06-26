using System.Security.Cryptography;
using Application.Features.Auth.Rules;
using Application.Persistence;
using Application.Services.SmsService;
using Core.Application.Pipelines.Logging;
using Core.Security.Hashing;
using Domain.Entities.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Features.Auth.Commands.SendOtp;

// Public endpoint: no ISecuredRequest (the AuthorizationBehavior requires an
// authenticated user). Only ILoggableRequest so the request is still logged.
public class SendOtpCommand : IRequest<SendOtpResponse>, ILoggableRequest
{
    public string PhoneNumber { get; set; } = default!;

    public class SendOtpCommandHandler : IRequestHandler<SendOtpCommand, SendOtpResponse>
    {
        private readonly IApplicationDbContext _db;
        private readonly ISmsSender _smsSender;
        private readonly AuthBusinessRules _authBusinessRules;
        private readonly OtpSettings _otpSettings;

        public SendOtpCommandHandler(
            IApplicationDbContext db,
            ISmsSender smsSender,
            AuthBusinessRules authBusinessRules,
            IOptions<OtpSettings> otpSettings)
        {
            _db = db;
            _smsSender = smsSender;
            _authBusinessRules = authBusinessRules;
            _otpSettings = otpSettings.Value;
        }

        public async Task<SendOtpResponse> Handle(SendOtpCommand request, CancellationToken cancellationToken)
        {
            string phone = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
            DateTime now = DateTime.UtcNow;

            // Rate limiting (server-side): cooldown between sends + daily cap per number.
            PhoneOtp? lastActive = await _db.PhoneOtps
                .Where(o => o.PhoneNumber == phone && !o.IsUsed && o.ExpiresAt > now)
                .OrderByDescending(o => o.CreatedDate)
                .FirstOrDefaultAsync(cancellationToken);
            await _authBusinessRules.OtpResendCooldownShouldBePassed(lastActive, _otpSettings.ResendCooldownSeconds, now);

            int sentToday = await _db.PhoneOtps
                .AsNoTracking()
                .CountAsync(o => o.PhoneNumber == phone && o.CreatedDate >= now.AddDays(-1), cancellationToken);
            await _authBusinessRules.OtpDailyLimitShouldNotBeExceeded(sentToday, _otpSettings.DailyLimit);

            // Invalidate any still-active code so verify only ever has one target.
            if (lastActive is not null)
            {
                lastActive.IsUsed = true;
                _db.PhoneOtps.Update(lastActive);
                await _db.SaveChangesAsync(cancellationToken);
            }

            string code = GenerateNumericCode(_otpSettings.CodeLength);
            HashingHelper.CreatePasswordHash(code, out byte[] codeHash, out byte[] codeSalt);

            PhoneOtp otp = new()
            {
                PhoneNumber = phone,
                CodeHash = codeHash,
                CodeSalt = codeSalt,
                ExpiresAt = now.AddMinutes(_otpSettings.ExpireMinutes),
                AttemptCount = 0,
                IsUsed = false
            };
            _db.PhoneOtps.Add(otp);
            await _db.SaveChangesAsync(cancellationToken);

            // The code is NEVER logged or returned. ConsoleSmsSender (dev) is the only
            // place it surfaces, behind the SMS abstraction.
            string message = $"Giris kodunuz: {code}. Kod {_otpSettings.ExpireMinutes} dakika gecerlidir.";
            await _smsSender.SendAsync(phone, message, cancellationToken);

            return new SendOtpResponse
            {
                ExpiresInSeconds = _otpSettings.ExpireMinutes * 60,
                ResendAvailableInSeconds = _otpSettings.ResendCooldownSeconds
            };
        }

        private static string GenerateNumericCode(int length)
        {
            // Cryptographically strong digits.
            char[] digits = new char[length];
            for (int i = 0; i < length; i++)
                digits[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
            return new string(digits);
        }
    }
}
