using Application.Services.Repositories;
using Core.Repositories;
using Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class EntitlementRepository : EfRepositoryBase<Entitlement, BaseDbContext>, IEntitlementRepository
{
    public EntitlementRepository(BaseDbContext context) : base(context) { }

    public async Task<Entitlement?> GetActiveByUserIdAsync(long userId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await Query()
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.IsActive && (e.CurrentPeriodEnd == null || e.CurrentPeriodEnd > nowUtc))
            .OrderByDescending(e => e.CurrentPeriodEnd)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Entitlement?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await Query()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
