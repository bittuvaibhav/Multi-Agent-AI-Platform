using Enterprise.Agent.Models.Enums;

namespace Enterprise.Agent.Models.Rag;

/// <summary>A source document submitted for ingestion into the RAG pipeline.</summary>
public sealed record DocumentIngestionRequest
{
    public required string DocumentId { get; init; }

    public required string FileName { get; init; }

    public required DocumentType DocumentType { get; init; }

    /// <summary>Raw file bytes; extraction is performed by the ingestion pipeline.</summary>
    public required byte[] Content { get; init; }

    public string? Collection { get; init; }

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();
}

/// <summary>A single chunk produced from a document after splitting.</summary>
public sealed record DocumentChunk
{
    public required string Id { get; init; }

    public required string DocumentId { get; init; }

    public required string Text { get; init; }

    public int Index { get; init; }

    public IReadOnlyList<float>? Embedding { get; init; }

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();
}

/// <summary>Outcome of ingesting a document.</summary>
public sealed record IngestionResult
{
    public required string DocumentId { get; init; }

    public int ChunkCount { get; init; }

    public int CharactersExtracted { get; init; }

    public string Collection { get; init; } = string.Empty;
}

/// <summary>A retrieved chunk with its similarity score.</summary>
public sealed record RetrievedChunk
{
    public required string Text { get; init; }

    public required string DocumentId { get; init; }

    public double Score { get; init; }

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();
}

/// <summary>The assembled context returned by a RAG query, ready for prompt injection.</summary>
public sealed record RagContext
{
    public required string Query { get; init; }

    public required IReadOnlyList<RetrievedChunk> Chunks { get; init; }

    /// <summary>The concatenated, citation-annotated context string.</summary>
    public string CombinedContext { get; init; } = string.Empty;

    public bool HasResults => Chunks.Count > 0;
}
