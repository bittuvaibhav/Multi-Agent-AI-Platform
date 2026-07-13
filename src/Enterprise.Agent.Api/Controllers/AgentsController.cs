using Enterprise.Agent.Api.Contracts;
using Enterprise.Agent.Core.Abstractions.Agents;
using Enterprise.Agent.Core.Abstractions.Orchestration;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Agent.Api.Controllers;

/// <summary>Introspects and directly invokes the registered agents, and exposes planning.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PlatformConstants.Policies.ApiKeyOrJwt)]
public sealed class AgentsController : ControllerBase
{
    private readonly IAgentRegistry _registry;
    private readonly IPlanner _planner;

    public AgentsController(IAgentRegistry registry, IPlanner planner)
    {
        _registry = registry;
        _planner = planner;
    }

    /// <summary>Lists all registered agents and their capabilities.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AgentDescriptor>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<AgentDescriptor>> List() => Ok(_registry.Descriptors);

    /// <summary>Produces a plan (which agents, in what order) for a goal without executing it.</summary>
    [HttpPost("plan")]
    public async Task<IActionResult> Plan([FromBody] PlanRequest request, CancellationToken cancellationToken)
    {
        var plan = await _planner.CreatePlanAsync(request.Goal, cancellationToken);
        return Ok(plan);
    }

    /// <summary>Invokes a single named agent directly.</summary>
    [HttpPost("{name}")]
    [ProducesResponseType(typeof(AgentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Invoke(
        string name, [FromBody] RunAgentRequest request, CancellationToken cancellationToken)
    {
        if (!_registry.TryGet(name, out var agent) || agent is null)
        {
            return NotFound(new { message = $"Agent '{name}' not found." });
        }

        var response = await agent.ExecuteAsync(
            new AgentRequest
            {
                Input = request.Input,
                Parameters = request.Parameters ?? new Dictionary<string, string>()
            },
            cancellationToken);

        return Ok(response);
    }
}
