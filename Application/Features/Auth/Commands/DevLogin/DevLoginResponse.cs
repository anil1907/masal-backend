using Core.Application.Responses;
using Core.Security.JWT;

namespace Application.Features.Auth.Commands.DevLogin;

public class DevLoginResponse : IResponse
{
    public AccessToken? AccessToken { get; set; }
    public string RefreshToken { get; set; } = "";
    public DateTime RefreshTokenExpiresAt { get; set; }
    public bool IsNewUser { get; set; }
}
