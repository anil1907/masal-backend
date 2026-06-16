using Application.Services.Repositories;
using Core.Repositories;
using Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Persistence.Contexts;

namespace Persistence.Repositories;

public class PhoneOtpRepository : EfRepositoryBase<PhoneOtp, BaseDbContext>, IPhoneOtpRepository
{
    public PhoneOtpRepository(BaseDbContext context) : base(context)
    {
    }

    public async Task<PhoneOtp?> GetLatestActiveAsync(string phoneNumber, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await Query()
            .AsNoTracking()
            .Where(o => o.PhoneNumber == phoneNumber && !o.IsUsed && o.ExpiresAt > nowUtc)
            .OrderByDescending(o => o.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CountCreatedSinceAsync(string phoneNumber, DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return await Query()
            .AsNoTracking()
            .CountAsync(o => o.PhoneNumber == phoneNumber && o.CreatedDate >= sinceUtc, cancellationToken);
    }
}
