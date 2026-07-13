using Enterprise.Agent.Models.Orchestration;

namespace Enterprise.Agent.Core.Abstractions.Orchestration;

/// <summary>
/// Executes workflow plans across agents. Supports sequential and parallel topologies,
/// per-step retries, timeouts, cancellation and full execution history.
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>Plans (via the registered planner) and then executes a goal end-to-end.</summary>
    Task<ExecutionHistory> RunGoalAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>Executes a pre-computed plan.</summary>
    Task<ExecutionHistory> ExecuteAsync(
        WorkflowPlan plan,
        AgentExecutionContext context,
        CancellationToken cancellationToken = default);
}
