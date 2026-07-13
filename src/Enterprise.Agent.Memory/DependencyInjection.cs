using Enterprise.Agent.Core.Abstractions.Memory;
using Enterprise.Agent.Memory.Options;
using Enterprise.Agent.Memory.Redis;
using Enterprise.Agent.Memory.Semantic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Enterprise.Agent.Memory;

/// <summary>Registers conversation, long-term (Redis) and semantic (vector) memory.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddMemory(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<MemoryOptions>(configuration.GetSection(MemoryOptions.SectionName));

        // Lazily-connected multiplexer that does not throw at startup when Redis is unavailable.
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MemoryOptions>>().Value;
            var config = ConfigurationOptions.Parse(options.RedisConnectionString);
            config.AbortOnConnectFail = false;
            config.ConnectRetry = 3;
            return ConnectionMultiplexer.Connect(config);
        });

        services.AddSingleton<IConversationMemory, RedisConversationMemory>();
        services.AddSingleton<IMemoryStore, RedisMemoryStore>();
        services.AddSingleton<ISemanticMemory, VectorSemanticMemory>();

        return services;
    }
}
