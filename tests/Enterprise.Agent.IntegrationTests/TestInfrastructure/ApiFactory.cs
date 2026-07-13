using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Enterprise.Agent.IntegrationTests.TestInfrastructure;

/// <summary>
/// Boots the API in-process for integration testing with the LLM providers mocked, EF Core
/// swapped for the in-memory provider, and deterministic test configuration (API key + JWT key).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string TestApiKey = "integration-test-key";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:AutoMigrate"] = "false",
                ["Jwt:SigningKey"] = "integration-test-signing-key-please-change-0123456789",
                ["ApiKeys:Keys:" + TestApiKey] = "integration-tests"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Swap SQL Server for the in-memory provider.
            services.RemoveAll<DbContextOptions<AgentDbContext>>();
            services.RemoveAll<AgentDbContext>();
            services.AddDbContext<AgentDbContext>(o => o.UseInMemoryDatabase("integration-tests"));

            // Mock the AI providers so no network/API keys are required.
            services.RemoveAll<IKernelFactory>();
            services.RemoveAll<IChatCompletionProvider>();
            services.RemoveAll<IEmbeddingProvider>();
            services.AddSingleton<IKernelFactory, FakeKernelFactory>();
            services.AddSingleton<IChatCompletionProvider, FakeChatProvider>();
            services.AddSingleton<IEmbeddingProvider, FakeEmbeddingProvider>();
        });
    }
}
