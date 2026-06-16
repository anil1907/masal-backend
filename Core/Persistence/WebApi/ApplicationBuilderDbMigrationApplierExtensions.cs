using Core.Persistence.DbMigrationApplier;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Persistence.WebApi;

public static class ApplicationBuilderDbMigrationApplierExtensions
{
    public static IApplicationBuilder UseDbMigrationApplier(this IApplicationBuilder app)
    {
        foreach (IDbMigrationApplierService service in app.ApplicationServices.GetServices<IDbMigrationApplierService>())
            service.Initialize();
        return app;
    }
}