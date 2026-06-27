using Application.Features.Account.Commands.DeleteAccount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AccountController : BaseController
{
    /// Permanently delete the authenticated user's account and all associated data
    /// (App Store Guideline 5.1.1(v)).
    [HttpDelete]
    public async Task<IActionResult> Delete()
        => Ok(await Mediator.Send(new DeleteAccountCommand()));
}
