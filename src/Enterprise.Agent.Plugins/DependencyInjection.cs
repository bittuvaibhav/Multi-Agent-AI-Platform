using Enterprise.Agent.Core.Abstractions.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace Enterprise.Agent.Plugins;

/// <summary>Registers the tool registry that catalogues and invokes plugins.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddToolRegistry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IToolRegistry, ToolRegistry>();
        return services;
    }
}
