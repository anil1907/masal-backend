using Application.Persistence;
using Application.Services.Token;
using Core.Security.Hashing;
using Core.Security.JWT;
using Domain.Entities.Auth;
using Domain.Entities.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Features.Auth.Commands.DevLogin;

// ⚠️ TEMPORARY TEST-ONLY ENDPOINT - REMOVE BEFORE PUBLIC LAUNCH.
// Logs in as a fixed throwaway test user (no Apple/SMS) so the app can be exercised in the
// simulator. Intentionally NOT gated (the user asked for it to work directly post-deploy);
// it grants a session ONLY for the isolated `dev_tester` account (no admin claims, no access
// to any other user's data). Delete this whole feature + the /api/Auth/dev-login route when
// real auth testing is done. Not ILoggableRequest (nothing useful to log).
public class DevLoginCommand : IRequest<DevLoginResponse>
{
    public class DevLoginCommandHandler : IRequestHandler<DevLoginCommand, DevLoginResponse>
    {
        private readonly IApplicationDbContext _db;
        private readonly ITokenHelper _tokenHelper;
        private readonly TokenOptions _tokenOptions;

        public DevLoginCommandHandler(
            IApplicationDbContext db,
            ITokenHelper tokenHelper,
            IOptions<TokenOptions> tokenOptions)
        {
            _db = db;
            _tokenHelper = tokenHelper;
            _tokenOptions = tokenOptions.Value;
        }

        public async Task<DevLoginResponse> Handle(DevLoginCommand request, CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;

            // A FRESH throwaway user every call: each "Test girişi" starts at a clean slate
            // (no child yet -> onboarding shows -> first child creates fine, no free-plan cap).
            User user = await CreateTestUser(cancellationToken);
            const bool isNewUser = true;

            List<OperationClaim> operationClaims = await _db.UserOperationClaims
                .AsNoTracking()
                .Where(p => p.UserId == user!.Id)
                .Select(p => new OperationClaim { Id = p.OperationClaimId, Name = p.OperationClaim.Name })
                .ToListAsync(cancellationToken);
            AccessToken accessToken = _tokenHelper.CreateToken(user!, operationClaims);

            (string rawRefresh, string refreshHash) = RefreshTokenHelper.Generate();
            DateTime refreshExpiresAt = now.AddDays(_tokenOptions.RefreshTokenExpirationDays);
            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user!.Id,
                TokenHash = refreshHash,
                ExpiresAt = refreshExpiresAt
            });
            await _db.SaveChangesAsync(cancellationToken);

            return new DevLoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = rawRefresh,
                RefreshTokenExpiresAt = refreshExpiresAt,
                IsNewUser = isNewUser
            };
        }

        private async Task<User> CreateTestUser(CancellationToken cancellationToken)
        {
            string suffix = Guid.NewGuid().ToString("N")[..12];
            HashingHelper.CreatePasswordHash(Guid.NewGuid().ToString("N"), out byte[] hash, out byte[] salt);
            User user = new()
            {
                Username = $"dev_{suffix}",
                Email = $"dev_{suffix}@ninnio.test",
                PhoneNumber = null,
                PasswordHash = hash,
                PasswordSalt = salt
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);
            return user;
        }
    }
}
