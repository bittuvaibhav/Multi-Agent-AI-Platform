using Enterprise.Agent.Core.Abstractions.Agents;
using Enterprise.Agent.Core.Abstractions.Orchestration;
using Enterprise.Agent.Core.Options;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Models.Orchestration;
using Microsoft.Extensions.Options;

namespace Enterprise.Agent.Core.Orchestration;

/// <summary>
/// Deterministic planner that selects agents by matching their advertised keywords against
/// the goal. Used when the LLM planner is disabled and as the LLM planner's fallback. Always
/// produces a valid, non-empty plan (falling back to the configured default agent).
/// </summary>
public sealed class KeywordPlanner : IPlanner
{
    private readonly IAgentRegistry _registry;
    private readonly PlannerOptions _options;

    public KeywordPlanner(IAgentRegistry registry, IOptions<PlannerOptions> options)
    {
        _registry = registry;
        _options = options.Value;
    }

    public Task<WorkflowPlan> CreatePlanAsync(string goal, CancellationToken cancellationToken = default)
    {
        var normalized = (goal ?? string.Empty).ToLowerInvariant();

        var matches = _registry.Descriptors
            .Where(d => d.Role != AgentRole.Coordinator && d.Role != AgentRole.Planner)
            .Select(d => new
            {
                Descriptor = d,
                Score = d.Keywords.Count(k => normalized.Contains(k.ToLowerInvariant(), StringComparison.Ordinal))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Descriptor)
            .ToList();

        if (matches.Count == 0)
        {
            var fallback = _registry.TryGet(_options.FallbackAgent, out var agent) && agent is not null
                ? agent.Descriptor.Name
                : _registry.Descriptors.FirstOrDefault(d => d.Role == AgentRole.Research)?.Name
                  ?? _registry.Descriptors.First().Name;

            return Task.FromResult(Single(goal ?? string.Empty, fallback));
        }

        var steps = matches
            .Select((d, i) => new PlannedStep
            {
                AgentName = d.Name,
                Instruction = goal ?? string.Empty,
                Order = i + 1
            })
            .ToList();

        return Task.FromResult(new WorkflowPlan
        {
            Goal = goal ?? string.Empty,
            Mode = WorkflowMode.Sequential,
            Steps = steps,
            Rationale = "Keyword-matched agents selected deterministically."
        });
    }

    private static WorkflowPlan Single(string goal, string agentName) => new()
    {
        Goal = goal,
        Mode = WorkflowMode.Sequential,
        Steps = [new PlannedStep { AgentName = agentName, Instruction = goal, Order = 1 }],
        Rationale = "No keyword match; routed to the default agent."
    };
}
