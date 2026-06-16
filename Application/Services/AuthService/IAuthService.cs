using Core.Security.JWT;
using Domain.Entities.Users;

namespace Application.Services.AuthService;

public interface IAuthService
{
    Task<AccessToken> CreateAccessToken(User user);
} 