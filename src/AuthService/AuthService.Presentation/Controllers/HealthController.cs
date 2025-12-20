using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Presentation.Controllers;

[ApiController]
[Route("/")]
public sealed class HealthController : ControllerBase
{
    [HttpGet("/")]
    [AllowAnonymous]
    public IActionResult GetHealthRedirect()
    {
        return LocalRedirect("/health");
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "OK" });
    }
}