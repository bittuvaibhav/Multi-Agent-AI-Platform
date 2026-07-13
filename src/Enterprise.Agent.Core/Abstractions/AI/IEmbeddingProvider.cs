namespace Enterprise.Agent.Core.Abstractions.AI;

/// <summary>
/// Generates dense vector embeddings for text. Implemented in Infrastructure over the
/// configured Semantic Kernel embedding service.
/// </summary>
public interface IEmbeddingProvider
{
    string ProviderId { get; }

    /// <summary>Dimensionality of the vectors this provider produces.</summary>
    int Dimensions { get; }

    Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IReadOnlyList<float>>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);
}
