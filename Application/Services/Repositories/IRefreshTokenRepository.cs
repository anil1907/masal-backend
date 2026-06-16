using Core.Repositories;
using Domain.Entities.Auth;

namespace Application.Services.Repositories;

public interface IRefreshTokenRepository : IAsyncRepository<RefreshToken>, IRepository<RefreshToken>
{
    Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, DateTime nowUtc, CancellationToken cancellationToken = default);
}
