using Core.Application.Responses;
using Core.Security.JWT;

namespace Application.Features.Auth.Commands.Login;

public class UserLoggedResponse : IResponse
{
    public AccessToken? AccessToken { get; set; }
    public List<string> Claims { get; set; }


    public LoggedHttpResponse ToHttpResponse()
    {
        return new() { AccessToken = AccessToken,  Claims = Claims };
    }

    public class LoggedHttpResponse
    {
        public AccessToken? AccessToken { get; set; }
        public List<string> Claims { get; set; }
    }
}