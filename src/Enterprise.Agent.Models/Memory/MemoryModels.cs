using Enterprise.Agent.Models.Enums;

namespace Enterprise.Agent.Models.Memory;

/// <summary>A single stored memory item.</summary>
public sealed record MemoryRecord
{
    public required string Key { get; init; }

    public required string Content { get; init; }

    public MemoryScope Scope { get; init; } = MemoryScope.Conversation;

    public string? ConversationId { get; init; }

    public string? UserId { get; init; }

    public IReadOnlyDictionary<string, string> Tags { get; init; } =
        new Dictionary<string, string>();

    /// <summary>Optional embedding used for semantic recall.</summary>
    public IReadOnlyList<float>? Embedding { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public double RelevanceScore { get; init; }
}

/// <summary>Query used to recall memory records.</summary>
public sealed record MemoryQuery
{
    public string? ConversationId { get; init; }

    public string? UserId { get; init; }

    public MemoryScope? Scope { get; init; }

    /// <summary>Free-text used for semantic recall (embedded by the memory service).</summary>
    public string? SemanticQuery { get; init; }

    public int Limit { get; init; } = 10;

    public double MinScore { get; init; } = 0.0;
}
