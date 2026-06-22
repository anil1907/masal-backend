using Core.Repositories;
using Domain.Entities.Children;

namespace Application.Services.Repositories;

public interface IChildRepository : IAsyncRepository<Child>, IRepository<Child>
{
    /// The parent's active child (the one story generation targets). Falls back to the most
    /// recently created child if none is flagged active. Null if the parent has no children.
    Task<Child?> GetActiveForUserAsync(long userId, CancellationToken cancellationToken = default);

    /// All of the parent's children, newest first.
    Task<List<Child>> GetAllForUserAsync(long userId, CancellationToken cancellationToken = default);

    /// How many children the parent currently has (for the free/premium cap).
    Task<int> CountForUserAsync(long userId, CancellationToken cancellationToken = default);

    /// A specific child owned by this parent (tracked, for update/activate). Null if not theirs.
    Task<Child?> GetByIdForUserAsync(long childId, long userId, CancellationToken cancellationToken = default);

    /// Clear the active flag on all of the parent's children (before activating one).
    Task DeactivateAllForUserAsync(long userId, CancellationToken cancellationToken = default);
}
