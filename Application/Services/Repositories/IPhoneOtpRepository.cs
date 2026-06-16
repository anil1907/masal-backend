using Core.Repositories;
using Domain.Entities.Auth;

namespace Application.Services.Repositories;

public interface IPhoneOtpRepository : IAsyncRepository<PhoneOtp>, IRepository<PhoneOtp>
{
    Task<PhoneOtp?> GetLatestActiveAsync(string phoneNumber, DateTime nowUtc, CancellationToken cancellationToken = default);

    Task<int> CountCreatedSinceAsync(string phoneNumber, DateTime sinceUtc, CancellationToken cancellationToken = default);
}
