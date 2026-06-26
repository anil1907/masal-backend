using Application.Persistence;
using Application.Services.AppleAuth;
using Application.Services.Token;
using Core.Security.Hashing;
using Core.Security.JWT;
using Domain.Entities.Auth;
using Domain.Entities.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Features.Auth.Commands.AppleSignIn;

// Public endpoint (no ISecuredRequest): this is how an Apple user becomes authenticated.
// NOT ILoggableRequest - the identity token is sensitive and large.
public class AppleSignInCommand : IRequest<AppleSignInResponse>
{
    /// The identity token from Sign in with Apple (ASAuthorizationAppleIDCredential.identityToken).
    public string IdentityToken { get; set; } = default!;
    /// Apple only returns these on the first authorization; persisted if present.
    public string? Email { get; set; }
    public string? FullName { get; set; }

    public class AppleSignInCommandHandler : IRequestHandler<AppleSignInCommand, AppleSignInResponse>
    {
        private readonly IAppleAuthVerifier _appleVerifier;
        private readonly IApplicationDbContext _db;
        private readonly ITokenHelper _tokenHelper;
        private readonly TokenOptions _tokenOptions;

        public AppleSignInCommandHandler(
            IAppleAuthVerifier appleVerifier,
            IApplicationDbContext db,
            ITokenHelper tokenHelper,
            IOptions<TokenOptions> tokenOptions)
        {
            _appleVerifier = appleVerifier;
            _db = db;
            _tokenHelper = tokenHelper;
            _tokenOptions = tokenOptions.Value;
        }

        public async Task<AppleSignInResponse> Handle(AppleSignInCommand request, CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;

            AppleIdentity identity = await _appleVerifier.VerifyAsync(request.IdentityToken, cancellationToken);

            User? user = await _db.Users
                .FirstOrDefaultAsync(u => u.AppleUserId == identity.UserId, cancellationToken);
            bool isNewUser = user is null;
            if (isNewUser)
                user = await CreateAppleUser(identity, request.Email, cancellationToken);

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

            return new AppleSignInResponse
            {
                AccessToken = accessToken,
                RefreshToken = rawRefresh,
                RefreshTokenExpiresAt = refreshExpiresAt,
                IsNewUser = isNewUser
            };
        }

        private async Task<User> CreateAppleUser(AppleIdentity identity, string? emailFromClient, CancellationToken cancellationToken)
        {
            // Apple users have no password; satisfy the non-null hash columns with a throwaway secret.
            HashingHelper.CreatePasswordHash(Guid.NewGuid().ToString("N"), out byte[] hash, out byte[] salt);

            User user = new()
            {
                Username = $"apple_{identity.UserId}",
                Email = identity.Email ?? emailFromClient ?? string.Empty,
                PhoneNumber = null,
                AppleUserId = identity.UserId,
                PasswordHash = hash,
                PasswordSalt = salt
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);
            return user;
        }
    }
}
