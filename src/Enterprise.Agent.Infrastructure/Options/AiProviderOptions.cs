namespace Enterprise.Agent.Infrastructure.Options;

/// <summary>
/// Provider-agnostic AI configuration. No provider is hard-coded as the default: the active
/// provider is chosen from <see cref="Provider"/> if set, otherwise inferred from whichever
/// section carries credentials.
/// </summary>
public sealed class AiProviderOptions
{
    public const string SectionName = "Ai";

    /// <summary>"OpenAI" or "AzureOpenAI". When empty, the provider is inferred from configuration.</summary>
    public string Provider { get; set; } = string.Empty;

    public OpenAiOptions OpenAI { get; set; } = new();

    public AzureOpenAiOptions AzureOpenAI { get; set; } = new();

    /// <summary>Dimensionality of embeddings produced by the configured embedding model.</summary>
    public int EmbeddingDimensions { get; set; } = 1536;

    /// <summary>Resolves the effective provider id ("openai" or "azure-openai").</summary>
    public string ResolveProvider()
    {
        if (!string.IsNullOrWhiteSpace(Provider))
        {
            return Provider.Replace("-", string.Empty).ToLowerInvariant() switch
            {
                "azureopenai" or "azure" => "azure-openai",
                "openai" => "openai",
                _ => Provider.ToLowerInvariant()
            };
        }

        if (!string.IsNullOrWhiteSpace(AzureOpenAI.Endpoint) && !string.IsNullOrWhiteSpace(AzureOpenAI.ApiKey))
        {
            return "azure-openai";
        }

        if (!string.IsNullOrWhiteSpace(OpenAI.ApiKey))
        {
            return "openai";
        }

        // No credentials configured; default logical id so DI stays functional (calls will fail clearly).
        return "openai";
    }
}

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string ChatModel { get; set; } = "gpt-4o-mini";

    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    public string? OrganizationId { get; set; }
}

public sealed class AzureOpenAiOptions
{
    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ChatDeployment { get; set; } = "gpt-4o-mini";

    public string EmbeddingDeployment { get; set; } = "text-embedding-3-small";

    public string ApiVersion { get; set; } = "2024-10-21";
}
