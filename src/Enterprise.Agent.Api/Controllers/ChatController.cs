using Enterprise.Agent.Core.Application.Chat;
using Enterprise.Agent.Models.Chat;
using Enterprise.Agent.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Agent.Api.Controllers;

/// <summary>Conversational entry point; plans and runs the appropriate agent(s).</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PlatformConstants.Policies.ApiKeyOrJwt)]
public sealed class ChatController : ControllerBase
{
    private readonly ISender _sender;

    public ChatController(ISender sender) => _sender = sender;

    /// <summary>Sends a message to the multi-agent system and returns the synthesised answer.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatResponse>> Post(
        [FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new ProcessChatCommand(request), cancellationToken);
        return Ok(response);
    }
}
