using Core.Repositories;
using Domain.Entities.Stories;

namespace Application.Services.Repositories;

public interface IStorySeriesRepository : IAsyncRepository<StorySeries>, IRepository<StorySeries>
{
    /// The child's active series (the one tonight continues), or null if none yet.
    Task<StorySeries?> GetActiveForChildAsync(long childId, CancellationToken cancellationToken = default);

    /// All series for the child, newest first.
    Task<List<StorySeries>> GetAllForChildAsync(long childId, CancellationToken cancellationToken = default);

    /// A series scoped to the child (ownership check on activate).
    Task<StorySeries?> GetForChildAsync(long id, long childId, CancellationToken cancellationToken = default);

    /// Clear the active flag on all of the child's series (before activating/creating one).
    Task DeactivateAllForChildAsync(long childId, CancellationToken cancellationToken = default);
}
