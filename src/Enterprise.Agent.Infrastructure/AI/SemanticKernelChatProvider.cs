using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Infrastructure.Options;
using Enterprise.Agent.Models.Chat;
using Enterprise.Agent.Models.Enums;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Enterprise.Agent.Infrastructure.AI;

/// <summary>
/// Provider-agnostic chat completion built on Semantic Kernel. Resolves the kernel's
/// <see cref="IChatCompletionService"/> and executes the conversation.
/// </summary>
public sealed class SemanticKernelChatProvider : IChatCompletionProvider
{
    private readonly IKernelFactory _kernelFactory;

    public SemanticKernelChatProvider(IKernelFactory kernelFactory, IOptions<AiProviderOptions> options)
    {
        _kernelFactory = kernelFactory;
        ProviderId = options.Value.ResolveProvider();
    }

    public string ProviderId { get; }

    public async Task<string> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionSettings? settings = null,
        CancellationToken cancellationToken = default)
    {
        var kernel = _kernelFactory.Create(importPlugins: false);
        var chat = kernel.GetRequiredService<IChatCompletionService>();

        var history = new ChatHistory();
        foreach (var message in messages)
        {
            switch (message.Role)
            {
                case MessageRole.System:
                    history.AddSystemMessage(message.Content);
                    break;
                case MessageRole.Assistant:
                    history.AddAssistantMessage(message.Content);
                    break;
                case MessageRole.Tool:
                    history.AddMessage(AuthorRole.Tool, message.Content);
                    break;
                default:
                    history.AddUserMessage(message.Content);
                    break;
            }
        }

        settings ??= new ChatCompletionSettings();
        var executionSettings = new PromptExecutionSettings
        {
            ExtensionData = new Dictionary<string, object>
            {
                ["temperature"] = settings.Temperature,
                ["top_p"] = settings.TopP,
                ["max_tokens"] = settings.MaxTokens ?? 1024
            }
        };

        var result = await chat.GetChatMessageContentAsync(history, executionSettings, kernel, cancellationToken)
            .ConfigureAwait(false);
        return result.Content ?? string.Empty;
    }
}
