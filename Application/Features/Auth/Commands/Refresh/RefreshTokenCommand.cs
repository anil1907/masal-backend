using Application.Features.Auth.Rules;
using Application.Services.AuthService;
using Application.Services.Repositories;
using Application.Services.Token;
using Core.Application.Pipelines.Logging;
using Core.Security.JWT;
using Domain.Entities.Auth;
using Domain.Entities.Users;
using MediatR;
using Microsoft.Extensions.Options;

namespace Application.Features.Auth.Commands.Refresh;

// Public endpoint (no ISecuredRequest): callers arrive with an EXPIRED access token.
// The refresh token itself is the credential. Rotated on every use.
public class RefreshTokenCommand : IRequest<RefreshedTokenResponse>, ILoggableRequest
{
    public string RefreshToken { get; set; } = default!;

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshedTokenResponse>
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAuthService _authService;
        private readonly AuthBusinessRules _authBusinessRules;
        private readonly TokenOptions _tokenOptions;

        public RefreshTokenCommandHandler(
            IRefreshTokenRepository refreshTokenRepository,
            IUserRepository userRepository,
            IAuthService authService,
            AuthBusinessRules authBusinessRules,
            IOptions<TokenOptions> tokenOptions)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _userRepository = userRepository;
            _authService = authService;
            _authBusinessRules = authBusinessRules;
            _tokenOptions = tokenOptions.Value;
        }

        public async Task<RefreshedTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;
            string hash = RefreshTokenHelper.Hash(request.RefreshToken);

            RefreshToken? current = await _refreshTokenRepository.GetActiveByHashAsync(hash, now, cancellationToken);
            await _authBusinessRules.RefreshTokenShouldBeValid(current);

            User? user = await _userRepository.GetAsync(u => u.Id == current!.UserId, cancellationToken: cancellationToken);
            await _authBusinessRules.UserShouldBeExistsWhenSelected(user);

            // Rotate: revoke the used token, issue a fresh pair.
            current!.RevokedAt = now;
            await _refreshTokenRepository.UpdateAsync(current, cancellationToken);

            (string rawRefresh, string refreshHash) = RefreshTokenHelper.Generate();
            DateTime refreshExpiresAt = now.AddDays(_tokenOptions.RefreshTokenExpirationDays);
            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                UserId = user!.Id,
                TokenHash = refreshHash,
                ExpiresAt = refreshExpiresAt
            }, cancellationToken);

            AccessToken accessToken = await _authService.CreateAccessToken(user);

            return new RefreshedTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = rawRefresh,
                RefreshTokenExpiresAt = refreshExpiresAt
            };
        }
    }
}
