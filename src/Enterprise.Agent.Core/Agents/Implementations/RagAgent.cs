using Enterprise.Agent.Core.Abstractions.Agents;
using Enterprise.Agent.Core.Abstractions.Rag;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Enums;
using Microsoft.Extensions.Logging;

namespace Enterprise.Agent.Core.Agents.Implementations;

/// <summary>
/// Retrieval-Augmented Generation agent. Delegates to the (Semantic Kernel powered)
/// <see cref="IRagService"/> to retrieve grounded context and produce a cited answer.
/// </summary>
public sealed class RagAgent : IAgent
{
    private readonly IRagService _rag;
    private readonly ILogger<RagAgent> _logger;

    public RagAgent(IRagService rag, ILogger<RagAgent> logger)
    {
        _rag = rag;
        _logger = logger;
    }

    public AgentDescriptor Descriptor { get; } = new()
    {
        Name = "RagAgent",
        Role = AgentRole.Rag,
        Description = "Answers questions grounded in the ingested knowledge base with citations.",
        Capabilities = AgentCapability.Retrieval | AgentCapability.TextGeneration,
        Keywords = ["knowledge base", "documents", "retrieve", "rag", "according to", "based on the docs", "cite"]
    };

    public async Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            var collection = request.Parameters.TryGetValue("collection", out var c) ? c : null;
            var answer = await _rag.AnswerAsync(request.Input, collection, cancellationToken).ConfigureAwait(false);
            return AgentResponse.Ok(Descriptor.Name, answer);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RagAgent failed.");
            return AgentResponse.Fail(Descriptor.Name, ex.Message);
        }
    }
}
