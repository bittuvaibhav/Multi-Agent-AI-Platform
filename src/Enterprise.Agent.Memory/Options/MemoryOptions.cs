namespace Enterprise.Agent.Memory.Options;

/// <summary>Configuration for the memory subsystem (Redis + semantic/vector memory).</summary>
public sealed class MemoryOptions
{
    public const string SectionName = "Memory";

    public string RedisConnectionString { get; set; } = "localhost:6379";

    /// <summary>Key namespace prefix for all memory entries.</summary>
    public string KeyPrefix { get; set; } = "eaa";

    /// <summary>Sliding expiry (minutes) applied to conversation memory.</summary>
    public int ConversationTtlMinutes { get; set; } = 240;

    /// <summary>Vector collection used to store semantic/long-term memories.</summary>
    public string SemanticCollection { get; set; } = "memory";

    /// <summary>Embedding dimensions used when provisioning the semantic collection.</summary>
    public int EmbeddingDimensions { get; set; } = 1536;
}
