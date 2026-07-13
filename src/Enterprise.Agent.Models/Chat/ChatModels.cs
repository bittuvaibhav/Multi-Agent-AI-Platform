using Enterprise.Agent.Models.Enums;

namespace Enterprise.Agent.Models.Chat;

/// <summary>A single message within a conversation.</summary>
public sealed record ChatMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required MessageRole Role { get; init; }

    public required string Content { get; init; }

    public string? Name { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();
}

/// <summary>A persisted conversation grouping ordered messages.</summary>
public sealed record Conversation
{
    public required string Id { get; init; }

    public string? Title { get; init; }

    public string? UserId { get; init; }

    public IReadOnlyList<ChatMessage> Messages { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>Incoming request to the chat/orchestration endpoint.</summary>
public sealed record ChatRequest
{
    public required string Message { get; init; }

    public string? ConversationId { get; init; }

    /// <summary>When set, forces a specific agent instead of using the planner.</summary>
    public string? PreferredAgent { get; init; }

    public bool UseMemory { get; init; } = true;

    public bool UseRag { get; init; }
}

/// <summary>Response returned to the caller after an orchestration run.</summary>
public sealed record ChatResponse
{
    public required string ConversationId { get; init; }

    public required string Answer { get; init; }

    public string CorrelationId { get; init; } = string.Empty;

    public IReadOnlyList<string> AgentsInvoked { get; init; } = [];

    public IReadOnlyList<string> ToolsInvoked { get; init; } = [];
}
