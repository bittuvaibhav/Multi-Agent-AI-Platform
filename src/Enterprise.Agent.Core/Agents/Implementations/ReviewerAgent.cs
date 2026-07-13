using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Core.Agents;
using Enterprise.Agent.Core.Options;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Prompts.Abstractions;
using Microsoft.Extensions.Logging;

namespace Enterprise.Agent.Core.Agents.Implementations;

/// <summary>Critically reviews a draft and returns actionable feedback plus a corrected version.</summary>
public sealed class ReviewerAgent : AgentBase
{
    public ReviewerAgent(
        IKernelFactory kernelFactory, IPromptProvider prompts, AgentDefaults defaults,
        ILogger<ReviewerAgent> logger)
        : base(kernelFactory, prompts, defaults, logger)
    {
    }

    protected override string PromptName => "reviewer";

    public override AgentDescriptor Descriptor { get; } = new()
    {
        Name = "ReviewerAgent",
        Role = AgentRole.Reviewer,
        Description = "Reviews drafts for accuracy, clarity, structure and tone.",
        Capabilities = AgentCapability.Review | AgentCapability.TextGeneration,
        Keywords = ["review", "critique", "feedback", "improve", "proofread", "edit", "check"]
    };
}
