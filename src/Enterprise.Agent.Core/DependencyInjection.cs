using Enterprise.Agent.Core.Abstractions.Agents;
using Enterprise.Agent.Core.Abstractions.Orchestration;
using Enterprise.Agent.Core.Agents;
using Enterprise.Agent.Core.Agents.Implementations;
using Enterprise.Agent.Core.Application.Behaviors;
using Enterprise.Agent.Core.Options;
using Enterprise.Agent.Core.Orchestration;
using Enterprise.Agent.Prompts;
using Enterprise.Agent.Shared.Correlation;
using Enterprise.Agent.Shared.Time;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Enterprise.Agent.Core;

/// <summary>Composition root for the Core layer.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Options
        services.Configure<OrchestratorOptions>(configuration.GetSection(OrchestratorOptions.SectionName));
        services.Configure<PlannerOptions>(configuration.GetSection(PlannerOptions.SectionName));
        services.Configure<AgentDefaults>(configuration.GetSection(AgentDefaults.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AgentDefaults>>().Value);

        // Cross-cutting primitives
        services.AddScoped<ICorrelationContext, CorrelationContext>();
        services.AddSingleton<IClock>(SystemClock.Instance);

        // Prompt library
        services.AddPromptLibrary();

        // Agents + registry
        services.AddAgents();
        services.AddSingleton<IAgentRegistry, AgentRegistry>();

        // Planners + orchestrator
        services.AddSingleton<KeywordPlanner>();
        services.AddSingleton<IPlanner, LlmPlanner>();
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();

        // CQRS via MediatR + FluentValidation
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        return services;
    }

    /// <summary>Registers every first-party agent as an <see cref="IAgent"/>.</summary>
    public static IServiceCollection AddAgents(this IServiceCollection services)
    {
        services.AddSingleton<IAgent, CoordinatorAgent>();
        services.AddSingleton<IAgent, ResearchAgent>();
        services.AddSingleton<IAgent, SqlAgent>();
        services.AddSingleton<IAgent, RagAgent>();
        services.AddSingleton<IAgent, WriterAgent>();
        services.AddSingleton<IAgent, ReviewerAgent>();
        services.AddSingleton<IAgent, DocumentAgent>();
        services.AddSingleton<IAgent, EmailAgent>();
        services.AddSingleton<IAgent, CodeAgent>();
        services.AddSingleton<IAgent, SummarizerAgent>();
        services.AddSingleton<IAgent, AnalyticsAgent>();
        services.AddSingleton<IAgent, PlannerAgent>();
        return services;
    }
}
