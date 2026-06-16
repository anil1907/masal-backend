using Core.Repositories;
using Domain.Entities.Children;

namespace Application.Services.Repositories;

public interface IChildRepository : IAsyncRepository<Child>, IRepository<Child>
{
    Task<Child?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}
