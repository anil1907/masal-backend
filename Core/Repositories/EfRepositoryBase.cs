using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Core.Persistence.Dynamic;
using Core.Persistence.Paging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Core.Repositories;

public class EfRepositoryBase<TEntity, TContext> : 
  IAsyncRepository<TEntity>,
  IQuery<TEntity>,
  IRepository<TEntity>
  where TEntity : Entity
  where TContext : DbContext
{
  protected readonly TContext Context;

  public EfRepositoryBase(TContext context) => Context = context;

  public IQueryable<TEntity> Query() => Context.Set<TEntity>();

  protected virtual void EditEntityPropertiesToAdd(TEntity entity)
  {
    entity.CreatedDate = DateTime.UtcNow;
  }

  public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
  {
    EditEntityPropertiesToAdd(entity);
    await Context.AddAsync(entity, cancellationToken);
    await Context.SaveChangesAsync(cancellationToken);
    return entity;
  }

  public async Task<ICollection<TEntity>> AddRangeAsync(
    ICollection<TEntity> entities,
    CancellationToken cancellationToken = default)
  {
    foreach (TEntity entity in entities)
      EditEntityPropertiesToAdd(entity);
    await Context.AddRangeAsync(entities, cancellationToken);
    await Context.SaveChangesAsync(cancellationToken);
    return entities;
  }

  protected virtual void EditEntityPropertiesToUpdate(TEntity entity)
  {
    entity.UpdatedDate = new DateTime?(DateTime.UtcNow);
  }

  public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
  {
    EditEntityPropertiesToUpdate(entity);
    Context.Update(entity);
    await Context.SaveChangesAsync(cancellationToken);
    return entity;
  }

  public async Task<ICollection<TEntity>> UpdateRangeAsync(
    ICollection<TEntity> entities,
    CancellationToken cancellationToken = default)
  {
    foreach (TEntity entity in entities)
      EditEntityPropertiesToUpdate(entity);
    Context.UpdateRange(entities);
    await Context.SaveChangesAsync(cancellationToken);
    return entities;
  }

  public async Task<TEntity> DeleteAsync(
    TEntity entity,
    bool permanent = false,
    CancellationToken cancellationToken = default)
  {
    await SetEntityAsDeleted(entity, permanent, cancellationToken: cancellationToken);
    await Context.SaveChangesAsync(cancellationToken);
    return entity;
  }

  public async Task<ICollection<TEntity>> DeleteRangeAsync(
    ICollection<TEntity> entities,
    bool permanent = false,
    CancellationToken cancellationToken = default)
  {
    await SetEntityAsDeleted(entities, permanent, cancellationToken: cancellationToken);
    await Context.SaveChangesAsync(cancellationToken);
    return entities;
  }

  public async Task<IPaginate<TEntity>> GetListAsync(
    Expression<Func<TEntity, bool>>? predicate = null,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
    int index = 0,
    int size = 10,
    bool withDeleted = false,
    bool enableTracking = true,
    CancellationToken cancellationToken = default)
  {
    IQueryable<TEntity> source = Query();
    if (!enableTracking)
      source = source.AsNoTracking();
    if (include != null)
      source = include(source);
    if (withDeleted)
      source = source.IgnoreQueryFilters();
    if (predicate != null)
      source = source.Where(predicate);
    if (orderBy == null)
      orderBy = q => q.OrderBy(e => e.Id); // Varsayılan sıralama
    return await orderBy(source).ToPaginateAsync(index, size, cancellationToken: cancellationToken);
  }

  public async Task<TEntity?> GetAsync(
    Expression<Func<TEntity, bool>> predicate,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
    bool withDeleted = false,
    bool enableTracking = true,
    CancellationToken cancellationToken = default)
  {
    IQueryable<TEntity> source = Query();
    if (!enableTracking)
      source = source.AsNoTracking();
    if (include != null)
      source = include(source);
    if (withDeleted)
      source = source.IgnoreQueryFilters();
    return await source.AsSplitQuery().FirstOrDefaultAsync(predicate, cancellationToken);
  }

  public async Task<IPaginate<TEntity>> GetListByDynamicAsync(
    DynamicQuery dynamic,
    Expression<Func<TEntity, bool>>? predicate = null,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
    int index = 0,
    int size = 10,
    bool withDeleted = false,
    bool enableTracking = true,
    CancellationToken cancellationToken = default)
  {
    IQueryable<TEntity> source = Query().ToDynamic<TEntity>(dynamic);
    if (!enableTracking)
      source = source.AsNoTracking();
    if (include != null)
      source = include(source);
    if (withDeleted)
      source = source.IgnoreQueryFilters();
    if (predicate != null)
      source = source.Where(predicate);
    return await source.ToPaginateAsync<TEntity>(index, size, cancellationToken: cancellationToken);
  }

  public async Task<bool> AnyAsync(
    Expression<Func<TEntity, bool>>? predicate = null,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
    bool withDeleted = false,
    CancellationToken cancellationToken = default)
  {
    IQueryable<TEntity> source = Query();
    if (withDeleted)
      source = source.IgnoreQueryFilters();
    if (predicate != null)
      source = source.Where(predicate);
    return await source.AnyAsync(cancellationToken);
  }

  public TEntity Add(TEntity entity)
  {
    EditEntityPropertiesToAdd(entity);
    Context.Add(entity);
    Context.SaveChanges();
    return entity;
  }

  public ICollection<TEntity> AddRange(ICollection<TEntity> entities)
  {
    foreach (TEntity entity in entities)
      EditEntityPropertiesToAdd(entity);
    Context.AddRange(entities);
    Context.SaveChanges();
    return entities;
  }

  public TEntity Update(TEntity entity)
  {
    EditEntityPropertiesToAdd(entity);
    Context.Update(entity);
    Context.SaveChanges();
    return entity;
  }

  public ICollection<TEntity> UpdateRange(ICollection<TEntity> entities)
  {
    foreach (TEntity entity in entities)
      EditEntityPropertiesToAdd(entity);
    Context.UpdateRange(entities);
    Context.SaveChanges();
    return entities;
  }

  public TEntity Delete(TEntity entity, bool permanent = false)
  {
    SetEntityAsDeleted(entity, permanent, false).Wait();
    Context.SaveChanges();
    return entity;
  }

  public ICollection<TEntity> DeleteRange(ICollection<TEntity> entities, bool permanent = false)
  {
    SetEntityAsDeleted(entities, permanent, false).Wait();
    Context.SaveChanges();
    return entities;
  }

  public TEntity? Get(
    Expression<Func<TEntity, bool>> predicate,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
    bool withDeleted = false,
    bool enableTracking = true)
  {
    IQueryable<TEntity> source = Query();
    if (!enableTracking)
      source = source.AsNoTracking();
    if (include != null)
      source = include(source);
    if (withDeleted)
      source = source.IgnoreQueryFilters();
    return source.FirstOrDefault(predicate);
  }

  public IPaginate<TEntity> GetList(
    Expression<Func<TEntity, bool>>? predicate = null,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
    int index = 0,
    int size = 10,
    bool withDeleted = false,
    bool enableTracking = true)
  {
    IQueryable<TEntity> source = Query();
    if (!enableTracking)
      source = source.AsNoTracking();
    if (include != null)
      source = include(source);
    if (withDeleted)
      source = source.IgnoreQueryFilters();
    if (predicate != null)
      source = source.Where(predicate);
    return orderBy != null ? orderBy(source).ToPaginate<TEntity>(index, size) : source.ToPaginate<TEntity>(index, size);
  }

  public IPaginate<TEntity> GetListByDynamic(
    DynamicQuery dynamic,
    Expression<Func<TEntity, bool>>? predicate = null,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
    int index = 0,
    int size = 10,
    bool withDeleted = false,
    bool enableTracking = true)
  {
    IQueryable<TEntity> source = Query().ToDynamic<TEntity>(dynamic);
    if (!enableTracking)
      source = source.AsNoTracking();
    if (include != null)
      source = include(source);
    if (withDeleted)
      source = source.IgnoreQueryFilters();
    if (predicate != null)
      source = source.Where(predicate);
    return source.ToPaginate<TEntity>(index, size);
  }

  public bool Any(
    Expression<Func<TEntity, bool>>? predicate = null,
    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
    bool withDeleted = false)
  {
    IQueryable<TEntity> source = Query();
    if (withDeleted)
      source = source.IgnoreQueryFilters();
    if (predicate != null)
      source = source.Where(predicate);
    return source.Any();
  }

  protected async Task SetEntityAsDeleted(
    TEntity entity,
    bool permanent,
    bool isAsync = true,
    CancellationToken cancellationToken = default)
  {
    if (!permanent)
    {
      CheckHasEntityHaveOneToOneRelation(entity);
      if (isAsync)
        await setEntityAsSoftDeleted(entity, isAsync, cancellationToken);
      else
        setEntityAsSoftDeleted(entity, isAsync).Wait();
    }
    else
      Context.Remove(entity);
  }

  protected async Task SetEntityAsDeleted(
    IEnumerable<TEntity> entities,
    bool permanent,
    bool isAsync = true,
    CancellationToken cancellationToken = default)
  {
    foreach (TEntity entity in entities)
      await SetEntityAsDeleted(entity, permanent, isAsync, cancellationToken);
  }

  protected IQueryable<object>? GetRelationLoaderQuery(
    IQueryable query,
    Type navigationPropertyType)
  {
    MethodInfo methodInfo1 = query.Provider.GetType().GetMethods().First((Func<MethodInfo, bool>) (m => (object) m != null && m.Name == "CreateQuery" && m.IsGenericMethod));
    MethodInfo methodInfo2;
    if ((object) methodInfo1 == null)
      methodInfo2 = null;
    else
      methodInfo2 = methodInfo1.MakeGenericMethod(navigationPropertyType);
    if ((object) methodInfo2 == null)
      throw new InvalidOperationException("CreateQuery<TElement> method is not found in IQueryProvider.");
    return ((IQueryable<object>) methodInfo2.Invoke(query.Provider, [
      query.Expression
    ])).Where((Expression<Func<object, bool>>) (x => !((IEntityTimestamps) x).DeletedDate.HasValue));
  }

  protected void CheckHasEntityHaveOneToOneRelation(TEntity entity)
  {
    IForeignKey foreignKey = Context.Entry(entity).Metadata.GetForeignKeys().FirstOrDefault((Func<IForeignKey, bool>) (fk => fk.IsUnique && fk.PrincipalKey.Properties.All((Func<IProperty, bool>) (pk => Context.Entry(entity).Property(pk.Name).Metadata.IsPrimaryKey()))));
    if (foreignKey != null)
    {
      string name = foreignKey.PrincipalEntityType.ClrType.Name;
      string str = string.Join(", ", Context.Entry<TEntity>(entity).Metadata.FindPrimaryKey().Properties.Select<IProperty, string>((Func<IProperty, string>) (prop => prop.Name)));
      DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(158, 3);
      interpolatedStringHandler.AppendLiteral("Entity ");
      interpolatedStringHandler.AppendFormatted(entity.GetType().Name);
      interpolatedStringHandler.AppendLiteral(" has a one-to-one relationship with ");
      interpolatedStringHandler.AppendFormatted(name);
      interpolatedStringHandler.AppendLiteral(" via the primary key (");
      interpolatedStringHandler.AppendFormatted(str);
      interpolatedStringHandler.AppendLiteral("). Soft Delete causes problems if you try to create an entry again with the same foreign key.");
      throw new InvalidOperationException(interpolatedStringHandler.ToStringAndClear());
    }
  }

  protected virtual void EditEntityPropertiesToDelete(TEntity entity)
  {
    entity.DeletedDate = new DateTime?(DateTime.UtcNow);
  }

  protected virtual void EditRelationEntityPropertiesToCascadeSoftDelete(IEntityTimestamps entity)
  {
    entity.DeletedDate = new DateTime?(DateTime.UtcNow);
  }

  protected virtual bool IsSoftDeleted(IEntityTimestamps entity) => entity.DeletedDate.HasValue;

  private async Task setEntityAsSoftDeleted(
    IEntityTimestamps entity,
    bool isAsync = true,
    CancellationToken cancellationToken = default,
    bool isRoot = true)
  {
    if (IsSoftDeleted(entity))
      return;
    if (isRoot)
      EditEntityPropertiesToDelete((TEntity) entity);
    else
      EditRelationEntityPropertiesToCascadeSoftDelete(entity);
    foreach (INavigation navigation in Context.Entry(entity).Metadata.GetNavigations().Where((Func<INavigation, bool>) (x =>
             {
               bool flag;
               if (x != null && !x.IsOnDependent)
               {
                 IForeignKey foreignKey = x.ForeignKey;
                 if (foreignKey != null)
                 {
                   switch (foreignKey.DeleteBehavior)
                   {
                     case DeleteBehavior.Cascade:
                     case DeleteBehavior.ClientCascade:
                       flag = true;
                       goto label_5;
                   }
                 }
               }
               flag = false;
               label_5:
               return flag;
             })).ToList())
    {
      if (!navigation.TargetEntityType.IsOwned() && !(navigation.PropertyInfo == null))
      {
        object entity1 = navigation.PropertyInfo.GetValue(entity);
        if (navigation.IsCollection)
        {
          if (entity1 == null)
          {
            IQueryable query = Context.Entry(entity).Collection(navigation.PropertyInfo.Name).Query();
            if (isAsync)
            {
              IQueryable<object> relationLoaderQuery = GetRelationLoaderQuery(query, navigation.PropertyInfo.GetType());
              if (relationLoaderQuery != null)
                entity1 = await relationLoaderQuery.ToListAsync<object>(cancellationToken);
            }
            else
            {
              IQueryable<object> relationLoaderQuery = GetRelationLoaderQuery(query, navigation.PropertyInfo.GetType());
              entity1 = relationLoaderQuery != null ? relationLoaderQuery.ToList<object>() : (object) null;
            }
            if (entity1 == null)
              continue;
          }
          foreach (IEntityTimestamps entity2 in (IEnumerable) entity1)
            await setEntityAsSoftDeleted(entity2, isAsync, cancellationToken, false);
        }
        else
        {
          if (entity1 == null)
          {
            IQueryable query = Context.Entry<IEntityTimestamps>(entity).Reference(navigation.PropertyInfo.Name).Query();
            if (isAsync)
            {
              IQueryable<object> relationLoaderQuery = GetRelationLoaderQuery(query, navigation.PropertyInfo.GetType());
              if (relationLoaderQuery != null)
                entity1 = await relationLoaderQuery.FirstOrDefaultAsync<object>(cancellationToken);
            }
            else
            {
              IQueryable<object> relationLoaderQuery = GetRelationLoaderQuery(query, navigation.PropertyInfo.GetType());
              entity1 = relationLoaderQuery != null ? relationLoaderQuery.FirstOrDefault<object>() : null;
            }
            if (entity1 == null)
              continue;
          }
          await setEntityAsSoftDeleted((IEntityTimestamps) entity1, isAsync, cancellationToken, false);
        }
      }
    }
    Context.Update<IEntityTimestamps>(entity);
  }
}