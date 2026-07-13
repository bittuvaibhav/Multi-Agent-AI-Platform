using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace Enterprise.Agent.Infrastructure.AI;

/// <summary>Provider-agnostic embedding generation built on Semantic Kernel.</summary>
public sealed class SemanticKernelEmbeddingProvider : IEmbeddingProvider
{
    private readonly IKernelFactory _kernelFactory;

    public SemanticKernelEmbeddingProvider(IKernelFactory kernelFactory, IOptions<AiProviderOptions> options)
    {
        _kernelFactory = kernelFactory;
        var value = options.Value;
        ProviderId = value.ResolveProvider();
        Dimensions = value.EmbeddingDimensions;
    }

    public string ProviderId { get; }

    public int Dimensions { get; }

    public async Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await EmbedBatchAsync([text], cancellationToken).ConfigureAwait(false);
        return result.Count > 0 ? result[0] : [];
    }

    public async Task<IReadOnlyList<IReadOnlyList<float>>> EmbedBatchAsync(
        IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        var kernel = _kernelFactory.Create(importPlugins: false);
        var service = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

        var embeddings = await service.GenerateEmbeddingsAsync(texts.ToList(), kernel, cancellationToken)
            .ConfigureAwait(false);

        return embeddings.Select(e => (IReadOnlyList<float>)e.ToArray()).ToArray();
    }
}
