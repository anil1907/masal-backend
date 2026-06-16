namespace Core.Application.Pipelines.Authorization;

public interface IUserAuthorizationService
{
    Task<ICollection<string>> GetUserOperationClaimsAsync(string userId);
    Task<bool> HasAnyRoleAsync(string userId, string[] requiredRoles);
}



