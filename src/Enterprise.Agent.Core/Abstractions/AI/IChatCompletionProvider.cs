using Enterprise.Agent.Models.Chat;

namespace Enterprise.Agent.Core.Abstractions.AI;

/// <summary>Provider-agnostic settings for a single chat completion call.</summary>
public sealed record ChatCompletionSettings
{
    public double Temperature { get; init; } = 0.2;

    public double TopP { get; init; } = 1.0;

    public int? MaxTokens { get; init; }

    /// <summary>Optional model/deployment override; when null the provider default is used.</summary>
    public string? ModelId { get; init; }
}

/// <summary>
/// Abstraction over a chat-completion backend. Implemented in the Infrastructure layer by
/// wrapping a Semantic Kernel chat-completion service, keeping the provider (OpenAI, Azure
/// OpenAI, ...) selectable purely by configuration.
/// </summary>
public interface IChatCompletionProvider
{
    /// <summary>Identifier of the concrete provider (e.g. "openai", "azure-openai").</summary>
    string ProviderId { get; }

    Task<string> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionSettings? settings = null,
        CancellationToken cancellationToken = default);
}
