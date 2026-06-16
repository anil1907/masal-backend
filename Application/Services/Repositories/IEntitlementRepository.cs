using Core.Repositories;
using Domain.Entities.Subscriptions;

namespace Application.Services.Repositories;

public interface IEntitlementRepository : IAsyncRepository<Entitlement>, IRepository<Entitlement>
{
    Task<Entitlement?> GetActiveByUserIdAsync(long userId, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// The user's entitlement row (tracked, for upsert on a new purchase), or null if none yet.
    Task<Entitlement?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}
