using Enterprise.Agent.Models.Enums;

namespace Enterprise.Agent.Persistence.Entities;

/// <summary>Persisted conversation aggregate root.</summary>
public sealed class ConversationEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string? Title { get; set; }

    public string? UserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<MessageEntity> Messages { get; set; } = [];
}

/// <summary>A single persisted chat message belonging to a conversation.</summary>
public sealed class MessageEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ConversationId { get; set; } = string.Empty;

    public MessageRole Role { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? Name { get; set; }

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public ConversationEntity? Conversation { get; set; }
}

/// <summary>Metadata about a document ingested into the knowledge base.</summary>
public sealed class DocumentEntity
{
    public string Id { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public DocumentType DocumentType { get; set; }

    public string Collection { get; set; } = string.Empty;

    public int ChunkCount { get; set; }

    public int CharactersExtracted { get; set; }

    public DateTimeOffset IngestedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Audit record of an orchestration run.</summary>
public sealed class AgentExecutionEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string CorrelationId { get; set; } = string.Empty;

    public string Goal { get; set; } = string.Empty;

    public WorkflowMode Mode { get; set; }

    public ExecutionStatus Status { get; set; }

    public string FinalOutput { get; set; } = string.Empty;

    /// <summary>Serialised per-step history (JSON).</summary>
    public string StepsJson { get; set; } = "[]";

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset CompletedAt { get; set; }
}
