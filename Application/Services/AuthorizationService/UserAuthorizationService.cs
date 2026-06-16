using Application.Features;
using Application.Services.Repositories;
using Core.Application.Pipelines.Authorization;
using Core.Security.Constants;
using Microsoft.AspNetCore.Http;

namespace Application.Services.AuthorizationService;

public class UserAuthorizationService : IUserAuthorizationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserOperationClaimRepository _userOperationClaimRepository;
    private readonly IUserRepository _userRepository;

    public UserAuthorizationService(
        IHttpContextAccessor httpContextAccessor,
        IUserOperationClaimRepository userOperationClaimRepository,
        IUserRepository userRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _userOperationClaimRepository = userOperationClaimRepository;
        _userRepository = userRepository;
    }

    public async Task<ICollection<string>> GetUserOperationClaimsAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return new List<string>();

        if (long.TryParse(userId, out long userIdLong))
        {
            var user = await _userRepository.GetAsync(u => u.Id == userIdLong);
            if (user != null)
            {
                var claims = await _userOperationClaimRepository.GetOperationClaimsByUserIdAsync(userIdLong);
                return claims.Select(c => c.Name).ToList();
            }
        }

        return new List<string>();
    }

    public async Task<bool> HasAnyRoleAsync(string userId, string[] requiredRoles)
    {
        if (requiredRoles.Contains(OperationClaims.AllowAnonymous))
            return true;

        var userClaims = await GetUserOperationClaimsAsync(userId);

        if (userClaims.Contains(GeneralOperationClaims.Admin) ||
            userClaims.Contains(OperationClaims.GeneralAdmin))
            return true;

        return requiredRoles.Any(role => userClaims.Contains(role));
    }
}
