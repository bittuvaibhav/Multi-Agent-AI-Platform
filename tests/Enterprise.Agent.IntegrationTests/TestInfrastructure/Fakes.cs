using System.Runtime.CompilerServices;
using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Models.Chat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Enterprise.Agent.IntegrationTests.TestInfrastructure;

/// <summary>Fake chat-completion service used to build test kernels ("mock OpenAI").</summary>
public sealed class FakeChatCompletionService : IChatCompletionService
{
    private readonly string _response;

    public FakeChatCompletionService(string response) => _response = response;

    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ChatMessageContent>>(
            [new ChatMessageContent(AuthorRole.Assistant, _response)]);

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new StreamingChatMessageContent(AuthorRole.Assistant, _response);
        await Task.CompletedTask;
    }
}

/// <summary>Kernel factory backed by <see cref="FakeChatCompletionService"/>.</summary>
public sealed class FakeKernelFactory : IKernelFactory
{
    public const string CannedResponse = "This is a canned test response.";

    public Kernel Create(string? providerId = null, bool importPlugins = true)
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<IChatCompletionService>(new FakeChatCompletionService(CannedResponse));
        return builder.Build();
    }
}

/// <summary>Chat provider returning a canned answer (used by the planner during tests).</summary>
public sealed class FakeChatProvider : IChatCompletionProvider
{
    public string ProviderId => "fake";

    public Task<string> CompleteAsync(
        IReadOnlyList<ChatMessage> messages, ChatCompletionSettings? settings = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(FakeKernelFactory.CannedResponse);
}

/// <summary>Embedding provider returning deterministic fixed-size vectors.</summary>
public sealed class FakeEmbeddingProvider : IEmbeddingProvider
{
    public string ProviderId => "fake";

    public int Dimensions => 8;

    public Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<float>>(Enumerable.Repeat(0.1f, Dimensions).ToArray());

    public Task<IReadOnlyList<IReadOnlyList<float>>> EmbedBatchAsync(
        IReadOnlyList<string> texts, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IReadOnlyList<float>>>(
            texts.Select(_ => (IReadOnlyList<float>)Enumerable.Repeat(0.1f, Dimensions).ToArray()).ToArray());
}
