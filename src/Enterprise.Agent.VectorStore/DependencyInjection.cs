using Enterprise.Agent.Core.Abstractions.VectorStore;
using Enterprise.Agent.VectorStore.AzureSearch;
using Enterprise.Agent.VectorStore.Options;
using Enterprise.Agent.VectorStore.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Enterprise.Agent.VectorStore;

/// <summary>Registers the pluggable vector stores and the selecting factory.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddVectorStores(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<VectorStoreOptions>(configuration.GetSection(VectorStoreOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<VectorStoreOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<VectorStoreOptions>().Postgres);
        services.AddSingleton(sp => sp.GetRequiredService<VectorStoreOptions>().AzureAiSearch);

        services.AddHttpClient();

        services.AddSingleton<PostgresVectorStore>();
        services.AddSingleton<AzureAiSearchVectorStore>();
        services.AddSingleton<IVectorStoreFactory, VectorStoreFactory>();

        return services;
    }
}
