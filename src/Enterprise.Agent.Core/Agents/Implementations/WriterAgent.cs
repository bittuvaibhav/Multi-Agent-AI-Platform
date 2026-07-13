using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Core.Agents;
using Enterprise.Agent.Core.Options;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Prompts.Abstractions;
using Microsoft.Extensions.Logging;

namespace Enterprise.Agent.Core.Agents.Implementations;

/// <summary>Produces polished prose (articles, posts, documents) from a request and context.</summary>
public sealed class WriterAgent : AgentBase
{
    public WriterAgent(
        IKernelFactory kernelFactory, IPromptProvider prompts, AgentDefaults defaults,
        ILogger<WriterAgent> logger)
        : base(kernelFactory, prompts, defaults, logger)
    {
    }

    protected override string PromptName => "writer";

    public override AgentDescriptor Descriptor { get; } = new()
    {
        Name = "WriterAgent",
        Role = AgentRole.Writer,
        Description = "Writes polished, professional prose in the requested tone and format.",
        Capabilities = AgentCapability.TextGeneration,
        Keywords = ["write", "draft", "compose", "article", "blog", "post", "content", "letter"]
    };
}
