using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Core.Agents;
using Enterprise.Agent.Core.Options;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Prompts.Abstractions;
using Microsoft.Extensions.Logging;

namespace Enterprise.Agent.Core.Agents.Implementations;

/// <summary>Generates complete, idiomatic code for a request.</summary>
public sealed class CodeAgent : AgentBase
{
    public CodeAgent(
        IKernelFactory kernelFactory, IPromptProvider prompts, AgentDefaults defaults,
        ILogger<CodeAgent> logger)
        : base(kernelFactory, prompts, defaults, logger)
    {
    }

    protected override string PromptName => "code";

    public override AgentDescriptor Descriptor { get; } = new()
    {
        Name = "CodeAgent",
        Role = AgentRole.Code,
        Description = "Writes and refactors complete, idiomatic code.",
        Capabilities = AgentCapability.CodeGeneration | AgentCapability.TextGeneration,
        Keywords = ["code", "program", "function", "implement", "bug", "refactor", "script", "class", "api"]
    };
}
