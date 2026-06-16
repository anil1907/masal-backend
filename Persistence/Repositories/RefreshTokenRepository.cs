using Application.Services.Repositories;
using Core.Repositories;
using Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class RefreshTokenRepository : EfRepositoryBase<RefreshToken, BaseDbContext>, IRefreshTokenRepository
{
    public RefreshTokenRepository(BaseDbContext context) : base(context) { }

    public async Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash && r.RevokedAt == null && r.ExpiresAt > nowUtc,
                cancellationToken);
    }
}
