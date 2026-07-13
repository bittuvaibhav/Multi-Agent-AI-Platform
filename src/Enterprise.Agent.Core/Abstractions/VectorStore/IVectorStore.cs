using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Models.VectorStore;

namespace Enterprise.Agent.Core.Abstractions.VectorStore;

/// <summary>
/// A pluggable vector index. Concrete implementations (PostgreSQL/pgvector, Azure AI Search)
/// live in the VectorStore layer and are selected via <see cref="IVectorStoreFactory"/>.
/// </summary>
public interface IVectorStore
{
    VectorProvider Provider { get; }

    Task EnsureCollectionAsync(string collection, int dimensions, CancellationToken cancellationToken = default);

    Task UpsertAsync(IReadOnlyList<VectorRecord> records, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(VectorQuery query, CancellationToken cancellationToken = default);

    Task DeleteAsync(string collection, IReadOnlyList<string> ids, CancellationToken cancellationToken = default);
}

/// <summary>Resolves the active vector store implementation by configuration.</summary>
public interface IVectorStoreFactory
{
    IVectorStore Create(VectorProvider? provider = null);
}
