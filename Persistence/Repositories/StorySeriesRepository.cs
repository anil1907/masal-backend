using Application.Services.Repositories;
using Core.Repositories;
using Domain.Entities.Stories;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class StorySeriesRepository : EfRepositoryBase<StorySeries, BaseDbContext>, IStorySeriesRepository
{
    public StorySeriesRepository(BaseDbContext context) : base(context) { }

    public async Task<StorySeries?> GetActiveForChildAsync(long childId, CancellationToken cancellationToken = default)
    {
        return await Query()
            .AsNoTracking()
            .Where(s => s.ChildId == childId && s.IsActive)
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<StorySeries>> GetAllForChildAsync(long childId, CancellationToken cancellationToken = default)
    {
        return await Query()
            .AsNoTracking()
            .Where(s => s.ChildId == childId)
            .OrderByDescending(s => s.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<StorySeries?> GetForChildAsync(long id, long childId, CancellationToken cancellationToken = default)
    {
        return await Query()
            .FirstOrDefaultAsync(s => s.Id == id && s.ChildId == childId, cancellationToken);
    }

    public async Task DeactivateAllForChildAsync(long childId, CancellationToken cancellationToken = default)
    {
        await Query()
            .Where(s => s.ChildId == childId && s.IsActive)
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.IsActive, false), cancellationToken);
    }
}

