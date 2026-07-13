using Enterprise.Agent.Core.Abstractions.Tools;
using Enterprise.Agent.Tools.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Enterprise.Agent.Tools;

/// <summary>Registers the built-in Semantic Kernel tool plugins.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddTools(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<SearchToolOptions>(configuration.GetSection(SearchToolOptions.SectionName));
        services.Configure<WeatherToolOptions>(configuration.GetSection(WeatherToolOptions.SectionName));
        services.Configure<RestApiToolOptions>(configuration.GetSection(RestApiToolOptions.SectionName));
        services.Configure<FileToolOptions>(configuration.GetSection(FileToolOptions.SectionName));

        services.AddHttpClient();

        // Register each tool once as a concrete singleton and expose it as an IKernelPluginSource.
        services.AddSingleton<CalculatorTool>();
        services.AddSingleton<WeatherTool>();
        services.AddSingleton<SearchTool>();
        services.AddSingleton<RestApiTool>();
        services.AddSingleton<SqlTool>();
        services.AddSingleton<EmailTool>();
        services.AddSingleton<FileTool>();

        services.AddSingleton<IKernelPluginSource>(sp => sp.GetRequiredService<CalculatorTool>());
        services.AddSingleton<IKernelPluginSource>(sp => sp.GetRequiredService<WeatherTool>());
        services.AddSingleton<IKernelPluginSource>(sp => sp.GetRequiredService<SearchTool>());
        services.AddSingleton<IKernelPluginSource>(sp => sp.GetRequiredService<RestApiTool>());
        services.AddSingleton<IKernelPluginSource>(sp => sp.GetRequiredService<SqlTool>());
        services.AddSingleton<IKernelPluginSource>(sp => sp.GetRequiredService<EmailTool>());
        services.AddSingleton<IKernelPluginSource>(sp => sp.GetRequiredService<FileTool>());

        return services;
    }
}
