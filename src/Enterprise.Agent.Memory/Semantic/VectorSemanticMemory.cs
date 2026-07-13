using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Core.Abstractions.Memory;
using Enterprise.Agent.Core.Abstractions.VectorStore;
using Enterprise.Agent.Memory.Options;
using Enterprise.Agent.Models.Memory;
using Enterprise.Agent.Models.VectorStore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Agent.Memory.Semantic;

/// <summary>
/// Semantic (vector) memory. Stores memories as embeddings in the configured vector store and
/// recalls them by cosine similarity to a query embedding.
/// </summary>
public sealed class VectorSemanticMemory : ISemanticMemory
{
    private readonly IVectorStoreFactory _vectorStoreFactory;
    private readonly IEmbeddingProvider _embeddings;
    private readonly MemoryOptions _options;
    private readonly ILogger<VectorSemanticMemory> _logger;

    public VectorSemanticMemory(
        IVectorStoreFactory vectorStoreFactory,
        IEmbeddingProvider embeddings,
        IOptions<MemoryOptions> options,
        ILogger<VectorSemanticMemory> logger)
    {
        _vectorStoreFactory = vectorStoreFactory;
        _embeddings = embeddings;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RememberAsync(MemoryRecord record, CancellationToken cancellationToken = default)
    {
        var store = _vectorStoreFactory.Create();
        var embedding = record.Embedding
                        ?? await _embeddings.EmbedAsync(record.Content, cancellationToken).ConfigureAwait(false);

        await store.EnsureCollectionAsync(_options.SemanticCollection, embedding.Count, cancellationToken)
            .ConfigureAwait(false);

        var metadata = new Dictionary<string, string>(record.Tags)
        {
            ["scope"] = record.Scope.ToString(),
            ["key"] = record.Key
        };
        if (record.UserId is not null) metadata["userId"] = record.UserId;
        if (record.ConversationId is not null) metadata["conversationId"] = record.ConversationId;

        await store.UpsertAsync(
            [
                new VectorRecord
                {
                    Id = $"{record.Scope}:{record.Key}",
                    Embedding = embedding,
                    Content = record.Content,
                    Collection = _options.SemanticCollection,
                    Metadata = metadata
                }
            ],
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<MemoryRecord>> RecallAsync(MemoryQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.SemanticQuery))
        {
            return [];
        }

        var store = _vectorStoreFactory.Create();
        var embedding = await _embeddings.EmbedAsync(query.SemanticQuery!, cancellationToken).ConfigureAwait(false);

        var hits = await store.SearchAsync(
            new VectorQuery
            {
                Embedding = embedding,
                Collection = _options.SemanticCollection,
                TopK = query.Limit,
                MinScore = query.MinScore
            },
            cancellationToken).ConfigureAwait(false);

        return hits.Select(h => new MemoryRecord
        {
            Key = h.Metadata.TryGetValue("key", out var k) ? k : h.Id,
            Content = h.Content,
            Scope = MemoryScopeFrom(h.Metadata),
            UserId = h.Metadata.TryGetValue("userId", out var u) ? u : null,
            ConversationId = h.Metadata.TryGetValue("conversationId", out var c) ? c : null,
            RelevanceScore = h.Score
        }).ToArray();
    }

    private static Models.Enums.MemoryScope MemoryScopeFrom(IReadOnlyDictionary<string, string> metadata) =>
        metadata.TryGetValue("scope", out var s) && Enum.TryParse<Models.Enums.MemoryScope>(s, out var scope)
            ? scope
            : Models.Enums.MemoryScope.Semantic;
}
