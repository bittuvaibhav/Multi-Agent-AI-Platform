using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Core.Abstractions.Agents;
using Enterprise.Agent.Core.Options;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Prompts.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Enterprise.Agent.Core.Agents;

/// <summary>
/// Base class for prompt-driven agents. Provides the common Semantic Kernel execution
/// path: it builds a kernel from <see cref="IKernelFactory"/>, renders the agent's prompt
/// from the versioned prompt library and invokes the kernel's chat-completion service.
/// Specialised agents override the small extension points (prompt name, tokens, settings).
/// </summary>
public abstract class AgentBase : IAgent
{
    private readonly IKernelFactory _kernelFactory;
    private readonly AgentDefaults _defaults;

    protected AgentBase(
        IKernelFactory kernelFactory,
        IPromptProvider prompts,
        AgentDefaults defaults,
        ILogger logger)
    {
        _kernelFactory = kernelFactory;
        Prompts = prompts;
        _defaults = defaults;
        Logger = logger;
    }

    protected IPromptProvider Prompts { get; }

    protected ILogger Logger { get; }

    public abstract AgentDescriptor Descriptor { get; }

    /// <summary>Name of the prompt template this agent renders.</summary>
    protected abstract string PromptName { get; }

    /// <summary>Optional pinned prompt version; null uses the latest.</summary>
    protected virtual string? PromptVersion => null;

    /// <summary>Whether the kernel should import tool plugins for this agent.</summary>
    protected virtual bool UsesTools => false;

    public virtual async Task<AgentResponse> ExecuteAsync(
        AgentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            var prompt = Prompts.Render(PromptName, BuildTokens(request), PromptVersion);
            var output = await CompleteAsync(prompt, cancellationToken).ConfigureAwait(false);

            var metadata = new Dictionary<string, string>
            {
                ["prompt"] = $"{PromptName}:{PromptVersion ?? "latest"}",
                ["role"] = Descriptor.Role.ToString()
            };
            return AgentResponse.Ok(Descriptor.Name, output, metadata);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Agent {Agent} failed to execute.", Descriptor.Name);
            return AgentResponse.Fail(Descriptor.Name, ex.Message);
        }
    }

    /// <summary>Builds the token dictionary used to render the prompt.</summary>
    protected virtual IReadOnlyDictionary<string, string> BuildTokens(AgentRequest request) =>
        new Dictionary<string, string>
        {
            ["input"] = request.Input,
            ["goal"] = request.Input,
            ["context"] = request.Context ?? string.Empty
        };

    /// <summary>Runs a single chat completion through the kernel's chat service.</summary>
    protected async Task<string> CompleteAsync(string userPrompt, CancellationToken cancellationToken)
    {
        var kernel = _kernelFactory.Create(importPlugins: UsesTools);
        var chat = kernel.GetRequiredService<IChatCompletionService>();

        var history = new ChatHistory();
        if (Prompts.TryGet("system", out var system, "v1") && system is not null)
        {
            history.AddSystemMessage(system.Template);
        }

        history.AddUserMessage(userPrompt);

        var settings = new PromptExecutionSettings
        {
            ExtensionData = new Dictionary<string, object>
            {
                ["temperature"] = _defaults.Temperature,
                ["top_p"] = _defaults.TopP,
                ["max_tokens"] = _defaults.MaxTokens ?? 1024
            }
        };

        var result = await chat.GetChatMessageContentAsync(history, settings, kernel, cancellationToken)
            .ConfigureAwait(false);

        return result.Content ?? string.Empty;
    }
}
