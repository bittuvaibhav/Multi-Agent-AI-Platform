using Enterprise.Agent.Persistence.Options;
using Enterprise.Agent.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Enterprise.Agent.Persistence;

/// <summary>Registers the EF Core context and repositories.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<PersistenceOptions>(configuration.GetSection(PersistenceOptions.SectionName));

        var connectionString = configuration.GetSection(PersistenceOptions.SectionName)["ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Server=localhost;Database=EnterpriseAgent;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        services.AddDbContext<AgentDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(AgentDbContext).Assembly.FullName)));

        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IExecutionHistoryRepository, ExecutionHistoryRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();

        return services;
    }
}
