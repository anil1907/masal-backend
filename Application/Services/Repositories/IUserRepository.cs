using Core.Repositories;
using Domain.Entities.Users;

namespace Application.Services.Repositories;

public interface IUserRepository : IAsyncRepository<User>, IRepository<User>
{
    Task<User?> GetByPhoneNumber(string phoneNumber);
    Task<User?> GetByAppleUserId(string appleUserId);
}