using Application.Features.Subscriptions.Commands.Activate;
using Application.Features.Subscriptions.Queries.GetStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SubscriptionController : BaseController
{
    [HttpGet("status")]
    public async Task<IActionResult> Status()
        => Ok(await Mediator.Send(new GetSubscriptionStatusQuery()));

    /// Record a verified store purchase as a server entitlement (premium source of truth).
    [HttpPost("activate")]
    public async Task<IActionResult> Activate([FromBody] ActivateSubscriptionCommand command)
        => Ok(await Mediator.Send(command));
}
