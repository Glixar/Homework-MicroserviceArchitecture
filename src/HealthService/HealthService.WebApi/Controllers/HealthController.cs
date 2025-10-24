using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthService.WebApi.Controllers;

[ApiController]
[Route("/")]
public sealed class HealthController : ControllerBase
{
    [HttpGet("/")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHealthRedirect(CancellationToken ct)
    {
        return LocalRedirect("/health");
    }
    
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHealth(CancellationToken ct)
    {
        return Ok("{\"status\": \"OK\"}");
    }
}