using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Core.Agents;
using Enterprise.Agent.Core.Options;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Prompts.Abstractions;
using Microsoft.Extensions.Logging;

namespace Enterprise.Agent.Core.Agents.Implementations;

/// <summary>Drafts professional emails, including a subject line.</summary>
public sealed class EmailAgent : AgentBase
{
    public EmailAgent(
        IKernelFactory kernelFactory, IPromptProvider prompts, AgentDefaults defaults,
        ILogger<EmailAgent> logger)
        : base(kernelFactory, prompts, defaults, logger)
    {
    }

    protected override string PromptName => "email";

    protected override bool UsesTools => true;

    public override AgentDescriptor Descriptor { get; } = new()
    {
        Name = "EmailAgent",
        Role = AgentRole.Email,
        Description = "Drafts and (via tools) can send professional emails.",
        Capabilities = AgentCapability.Email | AgentCapability.TextGeneration,
        Keywords = ["email", "mail", "reply", "compose email", "send email", "message to"]
    };
}
