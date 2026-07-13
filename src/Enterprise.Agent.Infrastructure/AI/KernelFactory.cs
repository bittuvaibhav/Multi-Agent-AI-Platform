using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Core.Abstractions.Tools;
using Enterprise.Agent.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Enterprise.Agent.Infrastructure.AI;

/// <summary>
/// Builds Semantic Kernel <see cref="Kernel"/> instances configured for the active provider
/// (OpenAI or Azure OpenAI, selected purely by configuration) and imports the platform's tool
/// plugins for tool-calling scenarios.
/// </summary>
public sealed class KernelFactory : IKernelFactory
{
    private readonly AiProviderOptions _options;
    private readonly IEnumerable<IKernelPluginSource> _pluginSources;
    private readonly ILoggerFactory _loggerFactory;

    public KernelFactory(
        IOptions<AiProviderOptions> options,
        IEnumerable<IKernelPluginSource> pluginSources,
        ILoggerFactory loggerFactory)
    {
        _options = options.Value;
        _pluginSources = pluginSources;
        _loggerFactory = loggerFactory;
    }

    public Kernel Create(string? providerId = null, bool importPlugins = true)
    {
        var provider = providerId ?? _options.ResolveProvider();
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(_loggerFactory);

        switch (provider)
        {
            case "azure-openai":
                builder.AddAzureOpenAIChatCompletion(
                    _options.AzureOpenAI.ChatDeployment,
                    _options.AzureOpenAI.Endpoint,
                    _options.AzureOpenAI.ApiKey);
                builder.AddAzureOpenAITextEmbeddingGeneration(
                    _options.AzureOpenAI.EmbeddingDeployment,
                    _options.AzureOpenAI.Endpoint,
                    _options.AzureOpenAI.ApiKey);
                break;

            default: // "openai"
                builder.AddOpenAIChatCompletion(
                    _options.OpenAI.ChatModel,
                    _options.OpenAI.ApiKey,
                    _options.OpenAI.OrganizationId);
                builder.AddOpenAITextEmbeddingGeneration(
                    _options.OpenAI.EmbeddingModel,
                    _options.OpenAI.ApiKey,
                    _options.OpenAI.OrganizationId);
                break;
        }

        var kernel = builder.Build();

        if (importPlugins)
        {
            foreach (var source in _pluginSources)
            {
                var plugin = source.BuildPlugin();
                if (!kernel.Plugins.Contains(plugin.Name))
                {
                    kernel.Plugins.Add(plugin);
                }
            }
        }

        return kernel;
    }
}
