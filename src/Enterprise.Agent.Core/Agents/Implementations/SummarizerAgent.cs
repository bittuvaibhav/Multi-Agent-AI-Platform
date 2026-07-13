using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Core.Agents;
using Enterprise.Agent.Core.Options;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Prompts.Abstractions;
using Microsoft.Extensions.Logging;

namespace Enterprise.Agent.Core.Agents.Implementations;

/// <summary>Produces faithful, concise summaries of supplied content.</summary>
public sealed class SummarizerAgent : AgentBase
{
    public SummarizerAgent(
        IKernelFactory kernelFactory, IPromptProvider prompts, AgentDefaults defaults,
        ILogger<SummarizerAgent> logger)
        : base(kernelFactory, prompts, defaults, logger)
    {
    }

    protected override string PromptName => "summarizer";

    public override AgentDescriptor Descriptor { get; } = new()
    {
        Name = "SummarizerAgent",
        Role = AgentRole.Summarizer,
        Description = "Condenses long content into a faithful, concise summary.",
        Capabilities = AgentCapability.Summarization | AgentCapability.TextGeneration,
        Keywords = ["summarize", "summary", "tldr", "condense", "brief", "recap", "shorten"]
    };
}
