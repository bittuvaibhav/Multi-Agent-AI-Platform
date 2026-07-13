using Enterprise.Agent.Models.Agents;

namespace Enterprise.Agent.Core.Abstractions.Agents;

/// <summary>
/// The contract every agent in the platform implements. Agents are stateless with respect
/// to a single invocation and are driven by the orchestrator.
/// </summary>
public interface IAgent
{
    /// <summary>Static description used by the planner and the /agents API.</summary>
    AgentDescriptor Descriptor { get; }

    /// <summary>Convenience accessor for the agent's unique name.</summary>
    string Name => Descriptor.Name;

    /// <summary>Executes the agent against a single request.</summary>
    Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default);
}
