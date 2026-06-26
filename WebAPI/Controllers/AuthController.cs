using Application.Features.Auth.Commands.AppleSignIn;
using Application.Features.Auth.Commands.DevLogin;
using Application.Features.Auth.Commands.Login;
using Application.Features.Auth.Commands.Refresh;
using Application.Features.Auth.Commands.SendOtp;
using Application.Features.Auth.Commands.VerifyOtp;
using Core.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : BaseController
{
    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] UserForLoginDto userForLoginDto)
    {
        UserLoginCommand loginCommand = new() { UserForLoginDto = userForLoginDto, IpAddress = getIpAddress() };
        var result = await Mediator.Send(loginCommand);

        return Ok(result.ToHttpResponse());
    }

    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// Sign in with Apple: trade an Apple identity token for our JWT + refresh token.
    [HttpPost("apple")]
    public async Task<IActionResult> Apple([FromBody] AppleSignInCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    /// DEV/TEST ONLY: log in as a fixed test user (no Apple/SMS). Hard-gated behind the
    /// "DevAuth:Enabled" config flag, which must stay false in production.
    [HttpPost("dev-login")]
    public async Task<IActionResult> DevLogin()
    {
        var result = await Mediator.Send(new DevLoginCommand());
        return Ok(result);
    }
}