using Application.Features.Children.Commands.Create;
using Application.Features.Children.Commands.Update;
using Application.Features.Children.Queries.GetMy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ChildrenController : BaseController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChildCommand command)
        => Created("", await Mediator.Send(command));

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateChildCommand command)
        => Ok(await Mediator.Send(command));

    [HttpGet("me")]
    public async Task<IActionResult> GetMy()
        => Ok(await Mediator.Send(new GetMyChildQuery()));
}
