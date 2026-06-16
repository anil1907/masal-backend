using Core.Repositories;
using Domain.Entities.Stories;

namespace Application.Services.Repositories;

public interface IStoryGenerationLogRepository : IAsyncRepository<StoryGenerationLog>, IRepository<StoryGenerationLog>
{
    Task<int> CountForUserSinceAsync(long userId, DateTime sinceUtc, CancellationToken cancellationToken = default);
}
