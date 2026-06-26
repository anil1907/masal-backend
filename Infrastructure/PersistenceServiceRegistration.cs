using Application.Persistence;
using Core.Persistence.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Contexts;

namespace Infrastructure;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BaseDbContext>(options => options.UseNpgsql(configuration.GetSection("ConnectionString").Get<string>()));

        services.AddDbMigrationApplier(buildServices => buildServices.GetRequiredService<BaseDbContext>());

        // Handlers depend on the IApplicationDbContext contract; the concrete EF context backs it.
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<BaseDbContext>());

        return services;
    }
}
