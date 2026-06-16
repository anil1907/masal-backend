#nullable disable
using Microsoft.EntityFrameworkCore;

namespace Core.Persistence.DbMigrationApplier;

public interface IDbMigrationApplierService
{
    void Initialize();
}
    
public interface IDbMigrationApplierService<TDbContext> : IDbMigrationApplierService where TDbContext : DbContext
{
}