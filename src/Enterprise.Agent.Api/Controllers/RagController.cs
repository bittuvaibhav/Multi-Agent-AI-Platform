using Enterprise.Agent.Api.Contracts;
using Enterprise.Agent.Core.Abstractions.Rag;
using Enterprise.Agent.Core.Application.Rag;
using Enterprise.Agent.Models.Rag;
using Enterprise.Agent.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enterprise.Agent.Api.Controllers;

/// <summary>Retrieval-Augmented Generation: retrieve grounded context or answer questions.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PlatformConstants.Policies.ApiKeyOrJwt)]
public sealed class RagController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IRagService _rag;

    public RagController(ISender sender, IRagService rag)
    {
        _sender = sender;
        _rag = rag;
    }

    /// <summary>Retrieves the most relevant chunks for a query.</summary>
    [HttpPost("query")]
    [ProducesResponseType(typeof(RagContext), StatusCodes.Status200OK)]
    public async Task<ActionResult<RagContext>> Query(
        [FromBody] RagQueryRequest request, CancellationToken cancellationToken)
    {
        var context = await _sender.Send(
            new QueryKnowledgeBaseQuery(request.Query, request.Collection, request.TopK), cancellationToken);
        return Ok(context);
    }

    /// <summary>Retrieves context and returns a grounded, cited answer.</summary>
    [HttpPost("answer")]
    public async Task<IActionResult> Answer([FromBody] RagQueryRequest request, CancellationToken cancellationToken)
    {
        var answer = await _rag.AnswerAsync(request.Query, request.Collection, cancellationToken);
        return Ok(new { request.Query, answer });
    }
}
