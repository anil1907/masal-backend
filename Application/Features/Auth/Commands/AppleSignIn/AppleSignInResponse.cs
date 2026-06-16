using Core.Application.Responses;
using Core.Security.JWT;

namespace Application.Features.Auth.Commands.AppleSignIn;

public class AppleSignInResponse : IResponse
{
    public AccessToken? AccessToken { get; set; }
    public string RefreshToken { get; set; } = "";
    public DateTime RefreshTokenExpiresAt { get; set; }
    public bool IsNewUser { get; set; }
}
