using Application.Services.Repositories;
using Core.Repositories;
using Domain.Entities.Children;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class ChildRepository : EfRepositoryBase<Child, BaseDbContext>, IChildRepository
{
    public ChildRepository(BaseDbContext context) : base(context) { }

    public async Task<Child?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }
}
