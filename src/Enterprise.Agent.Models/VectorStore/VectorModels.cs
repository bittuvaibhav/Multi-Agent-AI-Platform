namespace Enterprise.Agent.Models.VectorStore;

/// <summary>A record stored in a vector index: an embedding plus payload/metadata.</summary>
public sealed record VectorRecord
{
    public required string Id { get; init; }

    public required IReadOnlyList<float> Embedding { get; init; }

    public required string Content { get; init; }

    public string Collection { get; init; } = "default";

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();
}

/// <summary>A similarity-search query against a vector index.</summary>
public sealed record VectorQuery
{
    public required IReadOnlyList<float> Embedding { get; init; }

    public string Collection { get; init; } = "default";

    public int TopK { get; init; } = 5;

    public double MinScore { get; init; } = 0.0;

    public IReadOnlyDictionary<string, string> Filter { get; init; } =
        new Dictionary<string, string>();
}

/// <summary>A hit returned from a vector similarity search.</summary>
public sealed record VectorSearchResult
{
    public required string Id { get; init; }

    public required string Content { get; init; }

    public double Score { get; init; }

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();
}
