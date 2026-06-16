using Application.Services.Repositories;
using Core.Repositories;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class UserRepository : EfRepositoryBase<User, BaseDbContext>, IUserRepository
{
    public UserRepository(BaseDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByUsername(string username)
    {
        return await Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetByEmail(string email)
    {
        return await Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByPhoneNumber(string phoneNumber)
    {
        return await Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
    }

    public async Task<User?> GetByAppleUserId(string appleUserId)
    {
        return await Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.AppleUserId == appleUserId);
    }
}