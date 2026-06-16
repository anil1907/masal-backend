// Decompiled with JetBrains decompiler
// Type: .IQueryableDynamicFilterExtensions
// Assembly: Core.Persistence, Version=1.1.1.0, Culture=neutral, PublicKeyToken=null
// MVID: 57BE62A3-4E6D-4FFC-A35E-75E1E9C10D56
// Assembly location: /Users/hgoksal/.nuget/packages/narchitecture.core.persistence/1.1.1/lib/net8.0/Core.Persistence.dll

#nullable enable
using System.Linq.Dynamic.Core;
using System.Runtime.CompilerServices;
using System.Text;

namespace Core.Persistence.Dynamic;

public static class IQueryableDynamicFilterExtensions
{
  private static readonly string[] _orders =
  [
    "asc",
    "desc"
  ];
  private static readonly string[] _logics =
  [
    "and",
    "or"
  ];
  private static readonly IDictionary<string, string> _operators = new Dictionary<string, string>()
  {
    {
      "eq",
      "="
    },
    {
      "neq",
      "!="
    },
    {
      "lt",
      "<"
    },
    {
      "lte",
      "<="
    },
    {
      "gt",
      ">"
    },
    {
      "gte",
      ">="
    },
    {
      "isnull",
      "== null"
    },
    {
      "isnotnull",
      "!= null"
    },
    {
      "startswith",
      "StartsWith"
    },
    {
      "endswith",
      "EndsWith"
    },
    {
      "contains",
      "Contains"
    },
    {
      "doesnotcontain",
      "Contains"
    }
  };

  public static IQueryable<T> ToDynamic<T>(this IQueryable<T> query, DynamicQuery dynamicQuery)
  {
    if (dynamicQuery.Filter != null)
      query = Filter<T>(query, dynamicQuery.Filter);
    if (dynamicQuery.Sort != null && dynamicQuery.Sort.Any<Sort>())
      query = Sort<T>(query, dynamicQuery.Sort);
    return query;
  }

  private static IQueryable<T> Filter<T>(IQueryable<T> queryable, Filter filter)
  {
    IList<Filter> allFilters = GetAllFilters(filter);
    string[] array = allFilters.Select<Filter, string>((Func<Filter, string>) (f => f.Value)).ToArray<string>();
    string predicate = Transform(filter, allFilters);
    if (!string.IsNullOrEmpty(predicate) && array != null)
      queryable = queryable.Where<T>(predicate, array);
    return queryable;
  }

  private static IQueryable<T> Sort<T>(IQueryable<T> queryable, IEnumerable<Sort> sort)
  {
    foreach (Sort sort1 in sort)
    {
      if (string.IsNullOrEmpty(sort1.Field))
        throw new ArgumentException("Invalid Field");
      if (string.IsNullOrEmpty(sort1.Dir) || !_orders.Contains<string>(sort1.Dir))
        throw new ArgumentException("Invalid Order Type");
    }
    if (!sort.Any<Sort>())
      return queryable;
    string ordering = string.Join(",", sort.Select<Sort, string>((Func<Sort, string>) (s => s.Field + " " + s.Dir)));
    return queryable.OrderBy<T>(ordering);
  }

  public static IList<Filter> GetAllFilters(Filter filter)
  {
    List<Filter> filters = [];
    GetFilters(filter, filters);
    return filters;
  }

  private static void GetFilters(Filter filter, IList<Filter> filters)
  {
    filters.Add(filter);
    if (filter.Filters == null || !filter.Filters.Any<Filter>())
      return;
    foreach (Filter filter1 in filter.Filters)
      GetFilters(filter1, filters);
  }

  public static string Transform(Filter filter, IList<Filter> filters)
  {
    if (string.IsNullOrEmpty(filter.Field))
      throw new ArgumentException("Invalid Field");
    if (string.IsNullOrEmpty(filter.Operator) || !_operators.ContainsKey(filter.Operator))
      throw new ArgumentException("Invalid Operator");
    int num = filters.IndexOf(filter);
    string str1 = _operators[filter.Operator];
    StringBuilder stringBuilder1 = new StringBuilder();
    if (!string.IsNullOrEmpty(filter.Value))
    {
      if (filter.Operator == "doesnotcontain")
      {
        StringBuilder stringBuilder2 = stringBuilder1;
        StringBuilder stringBuilder3 = stringBuilder2;
        StringBuilder.AppendInterpolatedStringHandler interpolatedStringHandler = new StringBuilder.AppendInterpolatedStringHandler(11, 3, stringBuilder2);
        interpolatedStringHandler.AppendLiteral("(!np(");
        interpolatedStringHandler.AppendFormatted(filter.Field);
        interpolatedStringHandler.AppendLiteral(").");
        interpolatedStringHandler.AppendFormatted(str1);
        interpolatedStringHandler.AppendLiteral("(@");
        interpolatedStringHandler.AppendFormatted(num.ToString());
        interpolatedStringHandler.AppendLiteral("))");
        ref StringBuilder.AppendInterpolatedStringHandler local = ref interpolatedStringHandler;
        stringBuilder3.Append(ref local);
      }
      else if (str1 == "StartsWith" || str1 == "EndsWith" || str1 == "Contains")
      {
        StringBuilder stringBuilder4 = stringBuilder1;
        StringBuilder stringBuilder5 = stringBuilder4;
        StringBuilder.AppendInterpolatedStringHandler interpolatedStringHandler = new StringBuilder.AppendInterpolatedStringHandler(10, 3, stringBuilder4);
        interpolatedStringHandler.AppendLiteral("(np(");
        interpolatedStringHandler.AppendFormatted(filter.Field);
        interpolatedStringHandler.AppendLiteral(").");
        interpolatedStringHandler.AppendFormatted(str1);
        interpolatedStringHandler.AppendLiteral("(@");
        interpolatedStringHandler.AppendFormatted(num.ToString());
        interpolatedStringHandler.AppendLiteral("))");
        ref StringBuilder.AppendInterpolatedStringHandler local = ref interpolatedStringHandler;
        stringBuilder5.Append(ref local);
      }
      else
      {
        StringBuilder stringBuilder6 = stringBuilder1;
        StringBuilder stringBuilder7 = stringBuilder6;
        StringBuilder.AppendInterpolatedStringHandler interpolatedStringHandler = new StringBuilder.AppendInterpolatedStringHandler(7, 3, stringBuilder6);
        interpolatedStringHandler.AppendLiteral("np(");
        interpolatedStringHandler.AppendFormatted(filter.Field);
        interpolatedStringHandler.AppendLiteral(") ");
        interpolatedStringHandler.AppendFormatted(str1);
        interpolatedStringHandler.AppendLiteral(" @");
        interpolatedStringHandler.AppendFormatted(num.ToString());
        ref StringBuilder.AppendInterpolatedStringHandler local = ref interpolatedStringHandler;
        stringBuilder7.Append(ref local);
      }
    }
    else
    {
      string str2 = filter.Operator;
      if (str2 == "isnull" || str2 == "isnotnull")
      {
        StringBuilder stringBuilder8 = stringBuilder1;
        StringBuilder stringBuilder9 = stringBuilder8;
        StringBuilder.AppendInterpolatedStringHandler interpolatedStringHandler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder8);
        interpolatedStringHandler.AppendLiteral("np(");
        interpolatedStringHandler.AppendFormatted(filter.Field);
        interpolatedStringHandler.AppendLiteral(") ");
        interpolatedStringHandler.AppendFormatted(str1);
        ref StringBuilder.AppendInterpolatedStringHandler local = ref interpolatedStringHandler;
        stringBuilder9.Append(ref local);
      }
    }
    if (filter.Logic == null || filter.Filters == null || !filter.Filters.Any<Filter>())
      return stringBuilder1.ToString();
    if (!_logics.Contains<string>(filter.Logic))
      throw new ArgumentException("Invalid Logic");
    DefaultInterpolatedStringHandler interpolatedStringHandler1 = new DefaultInterpolatedStringHandler(4, 3);
    interpolatedStringHandler1.AppendFormatted<StringBuilder>(stringBuilder1);
    interpolatedStringHandler1.AppendLiteral(" ");
    interpolatedStringHandler1.AppendFormatted(filter.Logic);
    interpolatedStringHandler1.AppendLiteral(" (");
    interpolatedStringHandler1.AppendFormatted(string.Join(" " + filter.Logic + " ", filter.Filters.Select<Filter, string>((Func<Filter, string>) (f => Transform(f, filters))).ToArray<string>()));
    interpolatedStringHandler1.AppendLiteral(")");
    return interpolatedStringHandler1.ToStringAndClear();
  }
}