using System.Reflection;
using Application.Persistence;
using Core.Repositories;
using Domain.Entities.Auth;
using Domain.Entities.Children;
using Domain.Entities.Notifications;
using Domain.Entities.Stories;
using Domain.Entities.Subscriptions;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Contexts;

public class BaseDbContext : DbContext, IApplicationDbContext
{
    protected IConfiguration Configuration { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserOperationClaim> UserOperationClaims { get; set; }
    public DbSet<OperationClaim> OperationClaims { get; set; }
    public DbSet<PhoneOtp> PhoneOtps { get; set; }
    public DbSet<Child> Children { get; set; }
    public DbSet<Entitlement> Entitlements { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<StoryGenerationLog> StoryGenerationLogs { get; set; }
    public DbSet<StoryChapter> StoryChapters { get; set; }
    public DbSet<StorySeries> StorySeries { get; set; }
    public DbSet<DeviceToken> DeviceTokens { get; set; }

    public BaseDbContext(DbContextOptions dbContextOptions,
        IConfiguration configuration)
        : base(dbContextOptions)
    {
        Configuration = configuration;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override int SaveChanges()
    {
        StampTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // CreatedDate/UpdatedDate were stamped by the old repository base. With handlers writing through
    // the DbContext directly, the context owns this so the behavior is preserved everywhere.
    private void StampTimestamps()
    {
        DateTime utcNow = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<IEntityTimestamps>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedDate = utcNow;
            else if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedDate = utcNow;
        }
    }
}
