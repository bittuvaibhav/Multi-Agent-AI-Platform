using Enterprise.Agent.Api.Contracts;
using Enterprise.Agent.Core.Abstractions.Memory;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Models.Memory;
using Enterprise.Agent.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Agent.Api.Controllers;

/// <summary>Conversation, long-term and semantic memory operations.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PlatformConstants.Policies.ApiKeyOrJwt)]
public sealed class MemoryController : ControllerBase
{
    private readonly IConversationMemory _conversationMemory;
    private readonly ISemanticMemory _semanticMemory;

    public MemoryController(IConversationMemory conversationMemory, ISemanticMemory semanticMemory)
    {
        _conversationMemory = conversationMemory;
        _semanticMemory = semanticMemory;
    }

    /// <summary>Returns the most recent messages remembered for a conversation.</summary>
    [HttpGet("conversation/{conversationId}")]
    public async Task<IActionResult> GetConversation(
        string conversationId, [FromQuery] int limit, CancellationToken cancellationToken)
    {
        var records = await _conversationMemory.GetRecentAsync(
            conversationId, limit <= 0 ? 20 : limit, cancellationToken);
        return Ok(records);
    }

    /// <summary>Stores a semantic (vector) memory.</summary>
    [HttpPost("semantic")]
    public async Task<IActionResult> Remember([FromBody] RememberRequest request, CancellationToken cancellationToken)
    {
        await _semanticMemory.RememberAsync(new MemoryRecord
        {
            Key = request.Key,
            Content = request.Content,
            Scope = MemoryScope.Semantic,
            UserId = request.UserId,
            ConversationId = request.ConversationId
        }, cancellationToken);

        return Accepted();
    }

    /// <summary>Recalls semantic memories similar to a query.</summary>
    [HttpGet("semantic/recall")]
    public async Task<IActionResult> Recall(
        [FromQuery] string query, [FromQuery] int limit, CancellationToken cancellationToken)
    {
        var records = await _semanticMemory.RecallAsync(new MemoryQuery
        {
            SemanticQuery = query,
            Limit = limit <= 0 ? 10 : limit
        }, cancellationToken);

        return Ok(records);
    }
}
