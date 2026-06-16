using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HealthCheckController : BaseController
{
    [HttpGet]
    public  Task<IActionResult> Add()
    {

        return Task.FromResult<IActionResult>(Ok("Success"));
    }
}