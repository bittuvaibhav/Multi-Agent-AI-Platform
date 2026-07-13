using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Core.Abstractions.Rag;
using Enterprise.Agent.Core.Abstractions.Sql;
using Enterprise.Agent.Core.Abstractions.Tools;
using Enterprise.Agent.Infrastructure.AI;
using Enterprise.Agent.Infrastructure.Email;
using Enterprise.Agent.Infrastructure.Options;
using Enterprise.Agent.Infrastructure.Rag;
using Enterprise.Agent.Infrastructure.Rag.Extractors;
using Enterprise.Agent.Infrastructure.Sql;
using Enterprise.Agent.Memory;
using Enterprise.Agent.Plugins;
using Enterprise.Agent.Tools;
using Enterprise.Agent.VectorStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Enterprise.Agent.Infrastructure;

/// <summary>
/// Composition root for the Infrastructure layer. Wires the AI providers, RAG pipeline, SQL
/// agent, email transport and aggregates the vector store, memory and tool registrations.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AiProviderOptions>(configuration.GetSection(AiProviderOptions.SectionName));
        services.Configure<RagOptions>(configuration.GetSection(RagOptions.SectionName));
        services.Configure<SqlAgentOptions>(configuration.GetSection(SqlAgentOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));

        services.AddHttpClient();

        // AI providers (provider-agnostic: OpenAI or Azure OpenAI chosen by configuration).
        services.AddSingleton<IKernelFactory, KernelFactory>();
        services.AddSingleton<IChatCompletionProvider, SemanticKernelChatProvider>();
        services.AddSingleton<IEmbeddingProvider, SemanticKernelEmbeddingProvider>();

        // RAG pipeline
        services.AddSingleton<IDocumentChunker, DocumentChunker>();
        services.AddSingleton<IDocumentTextExtractor, PlainTextExtractor>();
        services.AddSingleton<IDocumentTextExtractor, PdfTextExtractor>();
        services.AddSingleton<IDocumentTextExtractor, WordTextExtractor>();
        services.AddSingleton<IRagService, RagService>();

        // SQL agent
        services.AddSingleton<ISqlSafetyValidator, SqlSafetyValidator>();
        services.AddSingleton<ISqlSchemaProvider, SqlSchemaProvider>();
        services.AddSingleton<ISqlExecutor, SqlExecutor>();
        services.AddSingleton<ISqlAgentService, SqlAgentService>();

        // Email transport
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        // Aggregate the remaining infrastructure concerns.
        services.AddTools(configuration);
        services.AddToolRegistry();
        services.AddVectorStores(configuration);
        services.AddMemory(configuration);

        return services;
    }
}
