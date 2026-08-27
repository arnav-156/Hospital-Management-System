using Hospital.Application.DTOs;
using Hospital.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController(ISystemHealthService healthService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<HealthStatusDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<HealthStatusDto>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthStatusDto>> GetAsync(CancellationToken cancellationToken)
    {
        var health = await healthService.GetHealthAsync(cancellationToken);
        return health.DatabaseConnected
            ? Ok(health)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, health);
    }
}
