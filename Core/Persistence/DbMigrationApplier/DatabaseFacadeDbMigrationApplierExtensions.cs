// Decompiled with JetBrains decompiler
// Type: NArchitecture.Core.Persistence.DbMigrationApplier.DatabaseFacadeDbMigrationApplierExtensions
// Assembly: Core.Persistence, Version=1.1.1.0, Culture=neutral, PublicKeyToken=null
// MVID: 57BE62A3-4E6D-4FFC-A35E-75E1E9C10D56
// Assembly location: /Users/hgoksal/.nuget/packages/narchitecture.core.persistence/1.1.1/lib/net8.0/Core.Persistence.dll

#nullable enable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Core.Persistence.DbMigrationApplier;

public static class DatabaseFacadeDbMigrationApplierExtensions
{
    public static DatabaseFacade EnsureDbApplied(this DatabaseFacade databaseFacade)
    {
        if (!databaseFacade.CanConnect())
            return databaseFacade;
        if (databaseFacade.IsInMemory())
            databaseFacade.EnsureCreated();
        if (databaseFacade.IsRelational())
            databaseFacade.Migrate();
        return databaseFacade;
    }
}