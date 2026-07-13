using Enterprise.Agent.Models.Enums;

namespace Enterprise.Agent.Models.Orchestration;

/// <summary>A single agent invocation the planner has decided to perform.</summary>
public sealed record PlannedStep
{
    public required string AgentName { get; init; }

    public required string Instruction { get; init; }

    public IReadOnlyDictionary<string, string> Parameters { get; init; } =
        new Dictionary<string, string>();

    /// <summary>Names of steps whose output this step depends on (for sequential chaining).</summary>
    public IReadOnlyList<string> DependsOn { get; init; } = [];

    public int Order { get; init; }
}

/// <summary>
/// The full plan produced by the planner: an ordered/parallel set of steps to fulfil a goal.
/// </summary>
public sealed record WorkflowPlan
{
    public required string Goal { get; init; }

    public WorkflowMode Mode { get; init; } = WorkflowMode.Sequential;

    public required IReadOnlyList<PlannedStep> Steps { get; init; }

    public string? Rationale { get; init; }

    public bool IsEmpty => Steps.Count == 0;
}
