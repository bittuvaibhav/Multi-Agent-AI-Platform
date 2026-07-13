using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Core.Agents;
using Enterprise.Agent.Core.Options;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Prompts.Abstractions;
using Microsoft.Extensions.Logging;

namespace Enterprise.Agent.Core.Agents.Implementations;

/// <summary>Gathers and organises factual information relevant to a request.</summary>
public sealed class ResearchAgent : AgentBase
{
    public ResearchAgent(
        IKernelFactory kernelFactory, IPromptProvider prompts, AgentDefaults defaults,
        ILogger<ResearchAgent> logger)
        : base(kernelFactory, prompts, defaults, logger)
    {
    }

    protected override string PromptName => "research";

    protected override bool UsesTools => true;

    public override AgentDescriptor Descriptor { get; } = new()
    {
        Name = "ResearchAgent",
        Role = AgentRole.Research,
        Description = "Investigates a topic and produces a factual, well-organised briefing.",
        Capabilities = AgentCapability.Retrieval | AgentCapability.WebSearch | AgentCapability.TextGeneration,
        Keywords = ["research", "investigate", "find", "gather", "information", "look up", "explain", "what is"]
    };
}
