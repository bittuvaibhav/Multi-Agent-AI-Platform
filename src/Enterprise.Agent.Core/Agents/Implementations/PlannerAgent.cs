using System.Text;
using System.Text.Json;
using Enterprise.Agent.Core.Abstractions.Agents;
using Enterprise.Agent.Core.Abstractions.Orchestration;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Enterprise.Agent.Core.Agents.Implementations;

/// <summary>
/// Exposes the planner as an agent: given a goal it returns the structured plan of which
/// agents would be invoked and in what order.
/// The planner is resolved lazily via the service provider to avoid a construction-time
/// dependency cycle (AgentRegistry → PlannerAgent → IPlanner → AgentRegistry).
/// </summary>
public sealed class PlannerAgent : IAgent
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PlannerAgent> _logger;

    public PlannerAgent(IServiceProvider serviceProvider, ILogger<PlannerAgent> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public AgentDescriptor Descriptor { get; } = new()
    {
        Name = "PlannerAgent",
        Role = AgentRole.Planner,
        Description = "Decomposes a goal into an ordered plan of agent steps.",
        Capabilities = AgentCapability.Planning,
        Keywords = ["plan", "steps", "break down", "decompose", "strategy"]
    };

    public async Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            var planner = _serviceProvider.GetRequiredService<IPlanner>();
            var plan = await planner.CreatePlanAsync(request.Input, cancellationToken).ConfigureAwait(false);

            var builder = new StringBuilder();
            builder.AppendLine($"Plan ({plan.Mode}): {plan.Rationale}");
            foreach (var step in plan.Steps.OrderBy(s => s.Order))
            {
                builder.AppendLine($"{step.Order}. {step.AgentName}: {step.Instruction}");
            }

            var metadata = new Dictionary<string, string>
            {
                ["mode"] = plan.Mode.ToString(),
                ["stepCount"] = plan.Steps.Count.ToString(),
                ["planJson"] = JsonSerializer.Serialize(plan, JsonOptions)
            };
            return AgentResponse.Ok(Descriptor.Name, builder.ToString().TrimEnd(), metadata);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PlannerAgent failed.");
            return AgentResponse.Fail(Descriptor.Name, ex.Message);
        }
    }
}
