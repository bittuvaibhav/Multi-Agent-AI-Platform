using Enterprise.Agent.Models.Enums;

namespace Enterprise.Agent.Models.Agents;

/// <summary>
/// Static description of an agent, surfaced to the planner and the /agents API.
/// </summary>
public sealed record AgentDescriptor
{
    public required string Name { get; init; }

    public required AgentRole Role { get; init; }

    public required string Description { get; init; }

    public AgentCapability Capabilities { get; init; } = AgentCapability.None;

    /// <summary>Free-form keywords the planner can match against a user goal.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = [];
}

/// <summary>
/// A unit of work handed to an agent. <see cref="Parameters"/> carries step-specific
/// arguments produced by the planner (for example a SQL question or a document id).
/// </summary>
public sealed record AgentRequest
{
    public required string Input { get; init; }

    public string? ConversationId { get; init; }

    public IReadOnlyDictionary<string, string> Parameters { get; init; } =
        new Dictionary<string, string>();

    /// <summary>Optional retrieved/aggregated context injected by prior steps.</summary>
    public string? Context { get; init; }

    public string? UserId { get; init; }

    public string? TenantId { get; init; }
}

/// <summary>The result produced by an agent for a single <see cref="AgentRequest"/>.</summary>
public sealed record AgentResponse
{
    public required string AgentName { get; init; }

    public required bool Success { get; init; }

    public string Output { get; init; } = string.Empty;

    public string? ErrorMessage { get; init; }

    /// <summary>Diagnostic metadata (tokens, model, tool calls, timings, ...).</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();

    public static AgentResponse Ok(string agentName, string output,
        IReadOnlyDictionary<string, string>? metadata = null) => new()
        {
            AgentName = agentName,
            Success = true,
            Output = output,
            Metadata = metadata ?? new Dictionary<string, string>()
        };

    public static AgentResponse Fail(string agentName, string error) => new()
    {
        AgentName = agentName,
        Success = false,
        ErrorMessage = error
    };
}
