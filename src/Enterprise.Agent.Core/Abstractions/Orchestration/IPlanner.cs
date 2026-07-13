using Enterprise.Agent.Models.Orchestration;

namespace Enterprise.Agent.Core.Abstractions.Orchestration;

/// <summary>
/// Decides which agent(s) to invoke for a goal and in what order/topology,
/// producing a <see cref="WorkflowPlan"/> for the orchestrator to execute.
/// </summary>
public interface IPlanner
{
    Task<WorkflowPlan> CreatePlanAsync(string goal, CancellationToken cancellationToken = default);
}
