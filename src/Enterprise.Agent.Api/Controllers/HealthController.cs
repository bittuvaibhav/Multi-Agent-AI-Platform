using Enterprise.Agent.Core.Abstractions.Agents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Agent.Api.Controllers;

/// <summary>Liveness and readiness information for the platform.</summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    private readonly IAgentRegistry _registry;

    public HealthController(IAgentRegistry registry) => _registry = registry;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new
    {
        status = "Healthy",
        service = "Enterprise Multi-Agent AI Platform",
        agents = _registry.All.Count,
        timestampUtc = DateTimeOffset.UtcNow
    });
}
