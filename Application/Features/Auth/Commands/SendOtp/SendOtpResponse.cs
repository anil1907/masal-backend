using Core.Application.Responses;

namespace Application.Features.Auth.Commands.SendOtp;

public class SendOtpResponse : IResponse
{
    public int ExpiresInSeconds { get; set; }
    public int ResendAvailableInSeconds { get; set; }
}
