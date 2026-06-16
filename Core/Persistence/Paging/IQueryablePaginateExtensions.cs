// Decompiled with JetBrains decompiler
// Type: NArchitecture.Core.Persistence.Paging.IQueryablePaginateExtensions
// Assembly: Core.Persistence, Version=1.1.1.0, Culture=neutral, PublicKeyToken=null
// MVID: 57BE62A3-4E6D-4FFC-A35E-75E1E9C10D56
// Assembly location: /Users/hgoksal/.nuget/packages/narchitecture.core.persistence/1.1.1/lib/net8.0/Core.Persistence.dll

#nullable enable
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace Core.Persistence.Paging;

public static class IQueryablePaginateExtensions
{
  public static async Task<IPaginate<T>> ToPaginateAsync<T>(
    this IQueryable<T> source,
    int index,
    int size,
    int from = 0,
    CancellationToken cancellationToken = default)
  {
    if (from > index)
    {
      throw new ArgumentException($"From: {from} > Index: {index}, must from <= Index");
    }
    int count = await source.CountAsync(cancellationToken).ConfigureAwait(false);
    List<T> objList = await Queryable.Skip<T>(source, (index - from) * size).Take<T>(size).ToListAsync<T>(cancellationToken).ConfigureAwait(false);
    return new Paginate<T>()
    {
      Index = index,
      Size = size,
      From = from,
      Count = count,
      Items = objList,
      Pages = (int) Math.Ceiling(count / (double) size)
    };
  }

  public static IPaginate<T> ToPaginate<T>(
    this IQueryable<T> source,
    int index,
    int size,
    int from = 0)
  {
    if (from > index)
    {
      DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(36, 2);
      interpolatedStringHandler.AppendLiteral("From: ");
      interpolatedStringHandler.AppendFormatted(from);
      interpolatedStringHandler.AppendLiteral(" > Index: ");
      interpolatedStringHandler.AppendFormatted(index);
      interpolatedStringHandler.AppendLiteral(", must from <= Index");
      throw new ArgumentException(interpolatedStringHandler.ToStringAndClear());
    }
    int num = Queryable.Count(source);
    List<T> list = Queryable.Take<T>(Queryable.Skip<T>(source, (index - from) * size), size).ToList<T>();
    return new Paginate<T>()
    {
      Index = index,
      Size = size,
      From = from,
      Count = num,
      Items = list,
      Pages = (int) Math.Ceiling(num / (double) size)
    };
  }
}