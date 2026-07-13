using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Core.Agents;
using Enterprise.Agent.Core.Options;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Prompts.Abstractions;
using Microsoft.Extensions.Logging;

namespace Enterprise.Agent.Core.Agents.Implementations;

/// <summary>
/// Synthesises the outputs of multiple agents into a single coherent answer. Typically the
/// final step of a multi-agent workflow.
/// </summary>
public sealed class CoordinatorAgent : AgentBase
{
    public CoordinatorAgent(
        IKernelFactory kernelFactory, IPromptProvider prompts, AgentDefaults defaults,
        ILogger<CoordinatorAgent> logger)
        : base(kernelFactory, prompts, defaults, logger)
    {
    }

    protected override string PromptName => "coordinator";

    public override AgentDescriptor Descriptor { get; } = new()
    {
        Name = "CoordinatorAgent",
        Role = AgentRole.Coordinator,
        Description = "Combines multiple agent outputs into one coherent, de-duplicated answer.",
        Capabilities = AgentCapability.Orchestration | AgentCapability.TextGeneration,
        Keywords = ["combine", "synthesize", "coordinate", "merge results"]
    };
}
