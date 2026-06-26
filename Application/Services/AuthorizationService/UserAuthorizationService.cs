using Application.Features;
using Application.Persistence;
using Core.Application.Pipelines.Authorization;
using Core.Security.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.AuthorizationService;

public class UserAuthorizationService : IUserAuthorizationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IApplicationDbContext _db;

    public UserAuthorizationService(
        IHttpContextAccessor httpContextAccessor,
        IApplicationDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    public async Task<ICollection<string>> GetUserOperationClaimsAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return new List<string>();

        if (long.TryParse(userId, out long userIdLong))
        {
            bool userExists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == userIdLong);
            if (userExists)
            {
                return await _db.UserOperationClaims
                    .AsNoTracking()
                    .Where(p => p.UserId == userIdLong)
                    .Select(p => p.OperationClaim.Name)
                    .ToListAsync();
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
