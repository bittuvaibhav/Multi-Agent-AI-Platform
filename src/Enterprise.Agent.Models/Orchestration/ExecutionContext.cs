using System.Collections.Concurrent;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Enums;

namespace Enterprise.Agent.Models.Orchestration;

/// <summary>
/// Mutable, thread-safe state shared across the steps of a single orchestration run.
/// Holds the correlation id, per-step outputs and an accumulating shared "scratchpad"
/// that later steps can read from.
/// </summary>
public sealed class AgentExecutionContext
{
    private readonly ConcurrentDictionary<string, string> _sharedState = new();
    private readonly ConcurrentDictionary<string, AgentResponse> _stepOutputs = new();

    public AgentExecutionContext(string correlationId, string goal, string? conversationId = null,
        string? userId = null, string? tenantId = null)
    {
        CorrelationId = correlationId;
        Goal = goal;
        ConversationId = conversationId;
        UserId = userId;
        TenantId = tenantId;
    }

    public string CorrelationId { get; }

    public string Goal { get; }

    public string? ConversationId { get; }

    public string? UserId { get; }

    public string? TenantId { get; }

    public IReadOnlyDictionary<string, string> SharedState => _sharedState;

    public IReadOnlyDictionary<string, AgentResponse> StepOutputs => _stepOutputs;

    public void SetState(string key, string value) => _sharedState[key] = value;

    public bool TryGetState(string key, out string? value)
    {
        var found = _sharedState.TryGetValue(key, out var v);
        value = v;
        return found;
    }

    public void RecordStepOutput(string stepName, AgentResponse response) =>
        _stepOutputs[stepName] = response;

    /// <summary>Concatenates all successful step outputs to feed downstream agents.</summary>
    public string BuildAggregatedContext()
    {
        var successful = _stepOutputs.Values
            .Where(r => r.Success && !string.IsNullOrWhiteSpace(r.Output))
            .Select(r => $"### {r.AgentName}\n{r.Output}");

        return string.Join("\n\n", successful);
    }
}

/// <summary>Immutable record of one executed step, retained for history/audit.</summary>
public sealed record ExecutionStep
{
    public required string AgentName { get; init; }

    public required string Instruction { get; init; }

    public ExecutionStatus Status { get; init; }

    public string? Output { get; init; }

    public string? Error { get; init; }

    public int Attempts { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset CompletedAt { get; init; }

    public TimeSpan Duration => CompletedAt - StartedAt;
}

/// <summary>The full outcome of an orchestration run, including per-step history.</summary>
public sealed record ExecutionHistory
{
    public required string CorrelationId { get; init; }

    public required string Goal { get; init; }

    public WorkflowMode Mode { get; init; }

    public ExecutionStatus Status { get; init; }

    public required IReadOnlyList<ExecutionStep> Steps { get; init; }

    public string FinalOutput { get; init; } = string.Empty;

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset CompletedAt { get; init; }

    public TimeSpan TotalDuration => CompletedAt - StartedAt;
}
