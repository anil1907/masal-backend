using Application.Services.Repositories;
using Core.Repositories;
using Domain.Entities.Children;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class ChildRepository : EfRepositoryBase<Child, BaseDbContext>, IChildRepository
{
    public ChildRepository(BaseDbContext context) : base(context) { }

    public async Task<Child?> GetActiveForUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        // Prefer the flagged-active child; fall back to the newest one so legacy single-child
        // accounts (created before IsActive existed) still resolve a hero.
        return await Query()
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IsActive)
            .ThenByDescending(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Child>> GetAllForUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await Query()
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountForUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await Query().CountAsync(c => c.UserId == userId, cancellationToken);
    }

    public async Task<Child?> GetByIdForUserAsync(long childId, long userId, CancellationToken cancellationToken = default)
    {
        return await Query()
            .FirstOrDefaultAsync(c => c.Id == childId && c.UserId == userId, cancellationToken);
    }

    public async Task DeactivateAllForUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        await Query()
            .Where(c => c.UserId == userId && c.IsActive)
            .ExecuteUpdateAsync(set => set.SetProperty(c => c.IsActive, false), cancellationToken);
    }
}
