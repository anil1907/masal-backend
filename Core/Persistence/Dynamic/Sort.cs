// Decompiled with JetBrains decompiler
// Type: NArchitecture.Core.Persistence.Dynamic.Sort
// Assembly: Core.Persistence, Version=1.1.1.0, Culture=neutral, PublicKeyToken=null
// MVID: 57BE62A3-4E6D-4FFC-A35E-75E1E9C10D56
// Assembly location: /Users/hgoksal/.nuget/packages/narchitecture.core.persistence/1.1.1/lib/net8.0/Core.Persistence.dll

#nullable enable
namespace Core.Persistence.Dynamic;

public class Sort
{
    public string Field { get; set; }

    public string Dir { get; set; }

    public Sort()
    {
        Field = string.Empty;
        Dir = string.Empty;
    }

    public Sort(string field, string dir)
    {
        Field = field;
        Dir = dir;
    }
}