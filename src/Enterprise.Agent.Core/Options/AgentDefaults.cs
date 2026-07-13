using Enterprise.Agent.Core.Abstractions.AI;

namespace Enterprise.Agent.Core.Options;

/// <summary>Default chat-completion settings applied by agents unless overridden.</summary>
public sealed class AgentDefaults
{
    public const string SectionName = "AgentDefaults";

    public double Temperature { get; set; } = 0.2;

    public double TopP { get; set; } = 1.0;

    public int? MaxTokens { get; set; } = 1024;

    public ChatCompletionSettings ToChatSettings() => new()
    {
        Temperature = Temperature,
        TopP = TopP,
        MaxTokens = MaxTokens
    };
}
