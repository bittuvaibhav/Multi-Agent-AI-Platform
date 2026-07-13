using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Core.Agents;
using Enterprise.Agent.Core.Options;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Prompts.Abstractions;
using Microsoft.Extensions.Logging;

namespace Enterprise.Agent.Core.Agents.Implementations;

/// <summary>Analyses, summarises, extracts from or restructures document content.</summary>
public sealed class DocumentAgent : AgentBase
{
    public DocumentAgent(
        IKernelFactory kernelFactory, IPromptProvider prompts, AgentDefaults defaults,
        ILogger<DocumentAgent> logger)
        : base(kernelFactory, prompts, defaults, logger)
    {
    }

    protected override string PromptName => "document";

    public override AgentDescriptor Descriptor { get; } = new()
    {
        Name = "DocumentAgent",
        Role = AgentRole.Document,
        Description = "Processes document content: summarise, extract or restructure.",
        Capabilities = AgentCapability.DocumentProcessing | AgentCapability.TextGeneration,
        Keywords = ["document", "extract", "parse", "pdf", "word", "file", "contract", "report"]
    };
}
