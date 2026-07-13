using Enterprise.Agent.Prompts.Abstractions;
using Enterprise.Agent.Prompts.Library;
using Enterprise.Agent.Prompts.Loading;
using Enterprise.Agent.Prompts.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace Enterprise.Agent.Prompts;

/// <summary>DI registration for the versioned prompt library.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the prompt library: built-in defaults first, then any embedded
    /// <c>*.prompt.md</c> resources (which may override the defaults by name+version).
    /// </summary>
    public static IServiceCollection AddPromptLibrary(this IServiceCollection services)
    {
        var registry = BuildRegistry();
        services.AddSingleton<IPromptProvider>(registry);
        return services;
    }

    /// <summary>Builds a fully populated registry (useful for tests and non-DI callers).</summary>
    public static PromptRegistry BuildRegistry()
    {
        var registry = new PromptRegistry();
        DefaultPromptLibrary.RegisterDefaults(registry);
        EmbeddedPromptLoader.LoadInto(registry);
        return registry;
    }
}
