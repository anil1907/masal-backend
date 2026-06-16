using Application.Services.Repositories;
using Core.Repositories;
using Domain.Entities.Stories;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class StoryChapterRepository : EfRepositoryBase<StoryChapter, BaseDbContext>, IStoryChapterRepository
{
    public StoryChapterRepository(BaseDbContext context) : base(context) { }

    public async Task<StoryChapter?> GetLatestForChildAsync(long childId, CancellationToken cancellationToken = default)
    {
        return await Query()
            .AsNoTracking()
            .Where(c => c.ChildId == childId)
            .OrderByDescending(c => c.Number)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<StoryChapter>> GetAllForChildAsync(long childId, CancellationToken cancellationToken = default)
    {
        return await Query()
            .AsNoTracking()
            .Where(c => c.ChildId == childId)
            .OrderByDescending(c => c.Number)
            .ToListAsync(cancellationToken);
    }

    public async Task<StoryChapter?> GetForChildAsync(long id, long childId, CancellationToken cancellationToken = default)
    {
        return await Query()
            .FirstOrDefaultAsync(c => c.Id == id && c.ChildId == childId, cancellationToken);
    }

    public async Task<int> CountForChildSinceAsync(long childId, DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return await Query()
            .AsNoTracking()
            .CountAsync(c => c.ChildId == childId && c.CreatedDate >= sinceUtc, cancellationToken);
    }
}
