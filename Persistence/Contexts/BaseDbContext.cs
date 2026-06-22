using System.Reflection;
using Domain.Entities.Auth;
using Domain.Entities.Children;
using Domain.Entities.Stories;
using Domain.Entities.Subscriptions;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Persistence.Contexts;

public class BaseDbContext : DbContext
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
}
