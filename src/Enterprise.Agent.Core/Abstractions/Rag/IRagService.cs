using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Models.Rag;

namespace Enterprise.Agent.Core.Abstractions.Rag;

/// <summary>End-to-end Retrieval-Augmented Generation service.</summary>
public interface IRagService
{
    Task<IngestionResult> IngestAsync(DocumentIngestionRequest request, CancellationToken cancellationToken = default);

    Task<RagContext> RetrieveAsync(
        string query, string? collection = null, int topK = 5, CancellationToken cancellationToken = default);

    /// <summary>Retrieves context and produces a grounded answer to the query.</summary>
    Task<string> AnswerAsync(
        string query, string? collection = null, CancellationToken cancellationToken = default);
}

/// <summary>Splits extracted document text into overlapping chunks suitable for embedding.</summary>
public interface IDocumentChunker
{
    IReadOnlyList<string> Chunk(string text, int maxChars, int overlapChars);
}

/// <summary>Extracts plain text from a raw document of a given type.</summary>
public interface IDocumentTextExtractor
{
    bool CanHandle(DocumentType documentType);

    Task<string> ExtractAsync(byte[] content, DocumentType documentType, CancellationToken cancellationToken = default);
}
