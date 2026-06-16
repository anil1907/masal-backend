using Application.Services.Repositories;
using Core.Repositories;
using Domain.Entities.Stories;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class StoryGenerationLogRepository : EfRepositoryBase<StoryGenerationLog, BaseDbContext>, IStoryGenerationLogRepository
{
    public StoryGenerationLogRepository(BaseDbContext context) : base(context) { }

    public async Task<int> CountForUserSinceAsync(long userId, DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return await Query()
            .AsNoTracking()
            .CountAsync(l => l.UserId == userId && l.CreatedDate >= sinceUtc, cancellationToken);
    }
}
