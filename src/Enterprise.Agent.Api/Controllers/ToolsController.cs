using Enterprise.Agent.Api.Contracts;
using Enterprise.Agent.Core.Abstractions.Tools;
using Enterprise.Agent.Models.Tools;
using Enterprise.Agent.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Agent.Api.Controllers;

/// <summary>Lists and directly invokes the registered Semantic Kernel tool plugins.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PlatformConstants.Policies.ApiKeyOrJwt)]
public sealed class ToolsController : ControllerBase
{
    private readonly IToolRegistry _tools;

    public ToolsController(IToolRegistry tools) => _tools = tools;

    /// <summary>Lists all available tool functions and their parameters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ToolDescriptor>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<ToolDescriptor>> List() => Ok(_tools.Descriptors);

    /// <summary>Invokes a specific tool function directly.</summary>
    [HttpPost("invoke")]
    [ProducesResponseType(typeof(ToolResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ToolResult>> Invoke(
        [FromBody] InvokeToolRequest request, CancellationToken cancellationToken)
    {
        var result = await _tools.InvokeAsync(new ToolInvocation
        {
            PluginName = request.Plugin,
            FunctionName = request.Function,
            Arguments = request.Arguments ?? new Dictionary<string, string>()
        }, cancellationToken);

        return Ok(result);
    }
}
