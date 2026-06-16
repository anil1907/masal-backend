using Application.Services.AppleAuth;
using Application.Services.AuthService;
using Application.Services.Repositories;
using Application.Services.Token;
using Core.Security.Hashing;
using Core.Security.JWT;
using Domain.Entities.Auth;
using Domain.Entities.Users;
using MediatR;
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
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IAuthService _authService;
        private readonly TokenOptions _tokenOptions;

        public AppleSignInCommandHandler(
            IAppleAuthVerifier appleVerifier,
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IAuthService authService,
            IOptions<TokenOptions> tokenOptions)
        {
            _appleVerifier = appleVerifier;
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _authService = authService;
            _tokenOptions = tokenOptions.Value;
        }

        public async Task<AppleSignInResponse> Handle(AppleSignInCommand request, CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;

            AppleIdentity identity = await _appleVerifier.VerifyAsync(request.IdentityToken, cancellationToken);

            User? user = await _userRepository.GetByAppleUserId(identity.UserId);
            bool isNewUser = user is null;
            if (isNewUser)
                user = await CreateAppleUser(identity, request.Email, cancellationToken);

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
            return await _userRepository.AddAsync(user, cancellationToken);
        }
    }
}
