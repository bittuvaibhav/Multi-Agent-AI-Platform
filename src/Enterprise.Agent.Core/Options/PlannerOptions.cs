namespace Enterprise.Agent.Core.Options;

/// <summary>Configuration for the planner.</summary>
public sealed class PlannerOptions
{
    public const string SectionName = "Planner";

    /// <summary>When true, use the LLM to plan; otherwise use the deterministic keyword planner.</summary>
    public bool UseLlmPlanner { get; set; } = true;

    /// <summary>Prompt name used for LLM planning.</summary>
    public string PromptName { get; set; } = "planner";

    /// <summary>Optional explicit prompt version (null = latest).</summary>
    public string? PromptVersion { get; set; }

    /// <summary>Agent to fall back to when planning yields no steps.</summary>
    public string FallbackAgent { get; set; } = "ResearchAgent";
}
