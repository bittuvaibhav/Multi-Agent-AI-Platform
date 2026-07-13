using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Enterprise.Agent.UnitTests.TestDoubles;

/// <summary>
/// A stand-in for a real chat-completion backend ("mock OpenAI"). Returns a canned response
/// (or one produced by a supplied delegate) so agents and services can be tested without any
/// network calls or API keys.
/// </summary>
public sealed class FakeChatCompletionService : IChatCompletionService
{
    private readonly Func<ChatHistory, string> _responder;

    public FakeChatCompletionService(string response) => _responder = _ => response;

    public FakeChatCompletionService(Func<ChatHistory, string> responder) => _responder = responder;

    public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();

    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var content = new ChatMessageContent(AuthorRole.Assistant, _responder(chatHistory));
        return Task.FromResult<IReadOnlyList<ChatMessageContent>>([content]);
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new StreamingChatMessageContent(AuthorRole.Assistant, _responder(chatHistory));
        await Task.CompletedTask;
    }
}
