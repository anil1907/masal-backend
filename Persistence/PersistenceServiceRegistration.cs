using Application.Services.Repositories;
using Core.Persistence.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Contexts;
using Persistence.Repositories;

namespace Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BaseDbContext>(options => options.UseNpgsql(configuration.GetSection("ConnectionString").Get<string>()));

        services.AddDbMigrationApplier(buildServices => buildServices.GetRequiredService<BaseDbContext>());
        services.AddScoped<IOperationClaimRepository, OperationClaimRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserOperationClaimRepository, UserOperationClaimRepository>();
        services.AddScoped<IPhoneOtpRepository, PhoneOtpRepository>();
        services.AddScoped<IChildRepository, ChildRepository>();
        services.AddScoped<IEntitlementRepository, EntitlementRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IStoryGenerationLogRepository, StoryGenerationLogRepository>();
        services.AddScoped<IStoryChapterRepository, StoryChapterRepository>();

        return services;
    }
}
