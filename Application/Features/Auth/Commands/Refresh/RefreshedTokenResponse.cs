using Core.Application.Responses;
using Core.Security.JWT;

namespace Application.Features.Auth.Commands.Refresh;

public class RefreshedTokenResponse : IResponse
{
    public AccessToken? AccessToken { get; set; }
    public string RefreshToken { get; set; } = "";
    public DateTime RefreshTokenExpiresAt { get; set; }
}
