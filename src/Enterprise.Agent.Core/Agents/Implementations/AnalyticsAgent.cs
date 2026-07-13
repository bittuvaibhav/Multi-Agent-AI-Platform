using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Core.Agents;
using Enterprise.Agent.Core.Options;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Prompts.Abstractions;
using Microsoft.Extensions.Logging;

namespace Enterprise.Agent.Core.Agents.Implementations;

/// <summary>Interprets data/metrics and surfaces insights, anomalies and recommendations.</summary>
public sealed class AnalyticsAgent : AgentBase
{
    public AnalyticsAgent(
        IKernelFactory kernelFactory, IPromptProvider prompts, AgentDefaults defaults,
        ILogger<AnalyticsAgent> logger)
        : base(kernelFactory, prompts, defaults, logger)
    {
    }

    protected override string PromptName => "analytics";

    public override AgentDescriptor Descriptor { get; } = new()
    {
        Name = "AnalyticsAgent",
        Role = AgentRole.Analytics,
        Description = "Analyses data and metrics to surface insights and recommendations.",
        Capabilities = AgentCapability.Analytics | AgentCapability.TextGeneration,
        Keywords = ["analyze", "analytics", "metrics", "trend", "insight", "data", "statistics", "kpi"]
    };
}
