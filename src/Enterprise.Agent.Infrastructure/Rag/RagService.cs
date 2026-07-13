using System.Text;
using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Core.Abstractions.Rag;
using Enterprise.Agent.Core.Abstractions.VectorStore;
using Enterprise.Agent.Infrastructure.Options;
using Enterprise.Agent.Models.Chat;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Models.Rag;
using Enterprise.Agent.Models.VectorStore;
using Enterprise.Agent.Prompts.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Agent.Infrastructure.Rag;

/// <summary>
/// End-to-end Retrieval-Augmented Generation service: ingest (extract → chunk → embed →
/// store), retrieve (embed query → similarity search → assemble cited context) and answer
/// (retrieve → prompt the model with grounded context).
/// </summary>
public sealed class RagService : IRagService
{
    private readonly IEnumerable<IDocumentTextExtractor> _extractors;
    private readonly IDocumentChunker _chunker;
    private readonly IEmbeddingProvider _embeddings;
    private readonly IVectorStoreFactory _vectorStoreFactory;
    private readonly IChatCompletionProvider _chat;
    private readonly IPromptProvider _prompts;
    private readonly RagOptions _options;
    private readonly ILogger<RagService> _logger;

    public RagService(
        IEnumerable<IDocumentTextExtractor> extractors,
        IDocumentChunker chunker,
        IEmbeddingProvider embeddings,
        IVectorStoreFactory vectorStoreFactory,
        IChatCompletionProvider chat,
        IPromptProvider prompts,
        IOptions<RagOptions> options,
        ILogger<RagService> logger)
    {
        _extractors = extractors;
        _chunker = chunker;
        _embeddings = embeddings;
        _vectorStoreFactory = vectorStoreFactory;
        _chat = chat;
        _prompts = prompts;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IngestionResult> IngestAsync(
        DocumentIngestionRequest request, CancellationToken cancellationToken = default)
    {
        var collection = request.Collection ?? _options.DefaultCollection;
        var extractor = _extractors.FirstOrDefault(e => e.CanHandle(request.DocumentType))
            ?? throw new NotSupportedException($"No extractor for document type {request.DocumentType}.");

        var text = await extractor.ExtractAsync(request.Content, request.DocumentType, cancellationToken)
            .ConfigureAwait(false);

        var chunks = _chunker.Chunk(text, _options.ChunkSize, _options.ChunkOverlap);
        if (chunks.Count == 0)
        {
            return new IngestionResult { DocumentId = request.DocumentId, ChunkCount = 0, Collection = collection };
        }

        var embeddings = await _embeddings.EmbedBatchAsync(chunks, cancellationToken).ConfigureAwait(false);

        var store = _vectorStoreFactory.Create();
        await store.EnsureCollectionAsync(collection, _embeddings.Dimensions, cancellationToken).ConfigureAwait(false);

        var records = new List<VectorRecord>(chunks.Count);
        for (var i = 0; i < chunks.Count; i++)
        {
            var metadata = new Dictionary<string, string>(request.Metadata)
            {
                ["documentId"] = request.DocumentId,
                ["fileName"] = request.FileName,
                ["chunkIndex"] = i.ToString()
            };
            records.Add(new VectorRecord
            {
                Id = $"{request.DocumentId}::{i}",
                Embedding = embeddings[i],
                Content = chunks[i],
                Collection = collection,
                Metadata = metadata
            });
        }

        await store.UpsertAsync(records, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "Ingested document {DocumentId}: {Chunks} chunks into '{Collection}'.",
            request.DocumentId, records.Count, collection);

        return new IngestionResult
        {
            DocumentId = request.DocumentId,
            ChunkCount = records.Count,
            CharactersExtracted = text.Length,
            Collection = collection
        };
    }

    public async Task<RagContext> RetrieveAsync(
        string query, string? collection = null, int topK = 5, CancellationToken cancellationToken = default)
    {
        var targetCollection = collection ?? _options.DefaultCollection;
        var embedding = await _embeddings.EmbedAsync(query, cancellationToken).ConfigureAwait(false);

        var store = _vectorStoreFactory.Create();
        var hits = await store.SearchAsync(
            new VectorQuery
            {
                Embedding = embedding,
                Collection = targetCollection,
                TopK = topK <= 0 ? _options.TopK : topK,
                MinScore = _options.MinScore
            },
            cancellationToken).ConfigureAwait(false);

        var chunks = hits.Select(h => new RetrievedChunk
        {
            Text = h.Content,
            DocumentId = h.Metadata.TryGetValue("documentId", out var d) ? d : h.Id,
            Score = h.Score,
            Metadata = h.Metadata
        }).ToArray();

        return new RagContext
        {
            Query = query,
            Chunks = chunks,
            CombinedContext = BuildCombinedContext(chunks)
        };
    }

    public async Task<string> AnswerAsync(
        string query, string? collection = null, CancellationToken cancellationToken = default)
    {
        var context = await RetrieveAsync(query, collection, _options.TopK, cancellationToken).ConfigureAwait(false);
        if (!context.HasResults)
        {
            return "I could not find relevant information in the knowledge base to answer that question.";
        }

        var prompt = _prompts.Render("rag-answer", new Dictionary<string, string>
        {
            ["question"] = query,
            ["context"] = context.CombinedContext
        });

        var messages = new List<ChatMessage>
        {
            new() { Role = MessageRole.User, Content = prompt }
        };

        return await _chat.CompleteAsync(messages, new ChatCompletionSettings { Temperature = 0.1 }, cancellationToken)
            .ConfigureAwait(false);
    }

    private static string BuildCombinedContext(IReadOnlyList<RetrievedChunk> chunks)
    {
        var builder = new StringBuilder();
        foreach (var chunk in chunks)
        {
            builder.Append('[').Append(chunk.DocumentId).Append("] ")
                .AppendLine(chunk.Text)
                .AppendLine();
        }

        return builder.ToString().Trim();
    }
}
