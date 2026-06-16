using Core.Repositories;
using Domain.Entities.Users;

namespace Domain.Entities.Subscriptions;

/// Server-side subscription entitlement. Presence of a row with CurrentPeriodEnd in the
/// future = premium. Created/refreshed by verified store transactions (Apple) - that
/// verification is a later, hardened step; for now this table is read by the status query.
public class Entitlement : Entity
{
    public long UserId { get; set; }
    public virtual User User { get; set; } = default!;

    public string Provider { get; set; } = "apple";          // apple | (future) other
    public string ProductId { get; set; } = default!;        // e.g. com.masal.app.monthly
    public DateTime? CurrentPeriodEnd { get; set; }
    public bool IsActive { get; set; }
}
