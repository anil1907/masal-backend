using Application.Features.Notifications.Commands.RegisterDevice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DevicesController : BaseController
{
    /// Register (or refresh) this device's APNs token so the server can push the user.
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDeviceCommand command)
        => Ok(await Mediator.Send(command));
}
