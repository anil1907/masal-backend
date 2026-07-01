using Domain.Entities.Auth;
using Domain.Entities.Children;
using Domain.Entities.Notifications;
using Domain.Entities.Stories;
using Domain.Entities.Subscriptions;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace Application.Persistence;

/// <summary>
/// Data-access contract the handlers depend on. Replaces the per-entity repositories:
/// handlers query and write through these DbSets directly (owner-scoped in the handler).
/// Implemented by the EF Core BaseDbContext in Persistence so the Application layer stays
/// free of a Persistence reference.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserOperationClaim> UserOperationClaims { get; }
    DbSet<OperationClaim> OperationClaims { get; }
    DbSet<PhoneOtp> PhoneOtps { get; }
    DbSet<Child> Children { get; }
    DbSet<Entitlement> Entitlements { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<StoryGenerationLog> StoryGenerationLogs { get; }
    DbSet<StoryChapter> StoryChapters { get; }
    DbSet<StorySeries> StorySeries { get; }
    DbSet<DeviceToken> DeviceTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
