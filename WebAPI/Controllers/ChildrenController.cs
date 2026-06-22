using Application.Features.Children.Commands.Activate;
using Application.Features.Children.Commands.Create;
using Application.Features.Children.Commands.Update;
using Application.Features.Children.Queries.GetList;
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

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await Mediator.Send(new GetChildrenQuery()));

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> Activate([FromRoute] long id)
        => Ok(await Mediator.Send(new ActivateChildCommand { Id = id }));
}
