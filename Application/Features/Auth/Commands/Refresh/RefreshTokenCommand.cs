using Application.Features.Auth.Rules;
using Application.Persistence;
using Application.Services.Token;
using Core.Application.Pipelines.Logging;
using Core.Security.JWT;
using Domain.Entities.Auth;
using Domain.Entities.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Features.Auth.Commands.Refresh;

// Public endpoint (no ISecuredRequest): callers arrive with an EXPIRED access token.
// The refresh token itself is the credential. Rotated on every use.
public class RefreshTokenCommand : IRequest<RefreshedTokenResponse>, ILoggableRequest
{
    public string RefreshToken { get; set; } = default!;

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshedTokenResponse>
    {
        private readonly IApplicationDbContext _db;
        private readonly ITokenHelper _tokenHelper;
        private readonly AuthBusinessRules _authBusinessRules;
        private readonly TokenOptions _tokenOptions;

        public RefreshTokenCommandHandler(
            IApplicationDbContext db,
            ITokenHelper tokenHelper,
            AuthBusinessRules authBusinessRules,
            IOptions<TokenOptions> tokenOptions)
        {
            _db = db;
            _tokenHelper = tokenHelper;
            _authBusinessRules = authBusinessRules;
            _tokenOptions = tokenOptions.Value;
        }

        public async Task<RefreshedTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;
            string hash = RefreshTokenHelper.Hash(request.RefreshToken);

            RefreshToken? current = await _db.RefreshTokens
                .FirstOrDefaultAsync(r => r.TokenHash == hash && r.RevokedAt == null && r.ExpiresAt > now, cancellationToken);
            await _authBusinessRules.RefreshTokenShouldBeValid(current);

            User? user = await _db.Users
                .FirstOrDefaultAsync(u => u.Id == current!.UserId, cancellationToken);
            await _authBusinessRules.UserShouldBeExistsWhenSelected(user);

            // Rotate: revoke the used token, issue a fresh pair.
            current!.RevokedAt = now;
            _db.RefreshTokens.Update(current);

            (string rawRefresh, string refreshHash) = RefreshTokenHelper.Generate();
            DateTime refreshExpiresAt = now.AddDays(_tokenOptions.RefreshTokenExpirationDays);
            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user!.Id,
                TokenHash = refreshHash,
                ExpiresAt = refreshExpiresAt
            });
            await _db.SaveChangesAsync(cancellationToken);

            // Build the access token (operation claims + JWT) inline.
            List<OperationClaim> operationClaims = await _db.UserOperationClaims
                .AsNoTracking()
                .Where(p => p.UserId == user.Id)
                .Select(p => new OperationClaim { Id = p.OperationClaimId, Name = p.OperationClaim.Name })
                .ToListAsync(cancellationToken);
            AccessToken accessToken = _tokenHelper.CreateToken(user, operationClaims);

            return new RefreshedTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = rawRefresh,
                RefreshTokenExpiresAt = refreshExpiresAt
            };
        }
    }
}
