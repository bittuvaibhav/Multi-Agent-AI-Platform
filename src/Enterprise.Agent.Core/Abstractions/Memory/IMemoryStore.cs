using Enterprise.Agent.Models.Memory;

namespace Enterprise.Agent.Core.Abstractions.Memory;

/// <summary>Low-level persistence of memory records (backing store agnostic).</summary>
public interface IMemoryStore
{
    Task SaveAsync(MemoryRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemoryRecord>> QueryAsync(MemoryQuery query, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>Short-lived, conversation-scoped memory (typically Redis-backed).</summary>
public interface IConversationMemory
{
    Task AppendAsync(string conversationId, MemoryRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemoryRecord>> GetRecentAsync(
        string conversationId, int limit = 20, CancellationToken cancellationToken = default);

    Task ClearAsync(string conversationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Long-lived semantic memory: stores content with embeddings and recalls it by
/// semantic similarity to a query.
/// </summary>
public interface ISemanticMemory
{
    Task RememberAsync(MemoryRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemoryRecord>> RecallAsync(
        MemoryQuery query, CancellationToken cancellationToken = default);
}
