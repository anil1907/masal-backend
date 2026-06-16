using Core.Repositories;
using Domain.Entities.Users;

namespace Application.Services.Repositories;

public interface IUserOperationClaimRepository : IAsyncRepository<UserOperationClaim>, IRepository<UserOperationClaim>
{
    Task<IList<OperationClaim>> GetOperationClaimsByUserIdAsync(long userId);
}
