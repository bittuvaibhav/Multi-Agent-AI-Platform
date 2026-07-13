using Enterprise.Agent.Core.Abstractions.VectorStore;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.VectorStore.AzureSearch;
using Enterprise.Agent.VectorStore.Options;
using Enterprise.Agent.VectorStore.Postgres;

namespace Enterprise.Agent.VectorStore;

/// <summary>Selects the active <see cref="IVectorStore"/> implementation by configuration.</summary>
public sealed class VectorStoreFactory : IVectorStoreFactory
{
    private readonly VectorStoreOptions _options;
    private readonly PostgresVectorStore _postgres;
    private readonly AzureAiSearchVectorStore _azure;

    public VectorStoreFactory(
        VectorStoreOptions options, PostgresVectorStore postgres, AzureAiSearchVectorStore azure)
    {
        _options = options;
        _postgres = postgres;
        _azure = azure;
    }

    public IVectorStore Create(VectorProvider? provider = null) => (provider ?? _options.Provider) switch
    {
        VectorProvider.AzureAiSearch => _azure,
        _ => _postgres
    };
}
