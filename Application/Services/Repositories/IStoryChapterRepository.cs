using Core.Repositories;
using Domain.Entities.Stories;

namespace Application.Services.Repositories;

public interface IStoryChapterRepository : IAsyncRepository<StoryChapter>, IRepository<StoryChapter>
{
    /// The most recent chapter in a series (highest Number), or null if the series is empty.
    Task<StoryChapter?> GetLatestForSeriesAsync(long seriesId, CancellationToken cancellationToken = default);

    /// All chapters for the child, newest first (library, across all series).
    Task<List<StoryChapter>> GetAllForChildAsync(long childId, CancellationToken cancellationToken = default);

    /// A single chapter scoped to the child (ownership check for mark-listened).
    Task<StoryChapter?> GetForChildAsync(long id, long childId, CancellationToken cancellationToken = default);

    /// Chapters created for the child since a cutoff (free-tier weekly allowance).
    Task<int> CountForChildSinceAsync(long childId, DateTime sinceUtc, CancellationToken cancellationToken = default);
}
