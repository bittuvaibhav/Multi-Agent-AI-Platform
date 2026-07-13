using Enterprise.Agent.Api.Hubs;
using Enterprise.Agent.Api.Middleware;
using Enterprise.Agent.Core;
using Enterprise.Agent.Infrastructure;
using Enterprise.Agent.Persistence;
using Enterprise.Agent.Persistence.Options;
using Enterprise.Agent.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;

// Bootstrap logger so failures during startup are captured.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/enterprise-agent-.log", rollingInterval: RollingInterval.Day));

    var config = builder.Configuration;

    // Layered composition roots.
    builder.Services.AddCore(config);
    builder.Services.AddInfrastructure(config);
    builder.Services.AddPersistence(config);
    builder.Services.AddPlatformSecurity(config);

    builder.Services.AddControllers();
    builder.Services.AddSignalR();
    builder.Services.AddHealthChecks();
    builder.Services.AddProblemDetails();

    builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials()));

    // OpenAPI document generation (native .NET 10), surfaced through the Scalar API reference UI.
    builder.Services.AddOpenApi("v1", options =>
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new OpenApiInfo
            {
                Title = "Enterprise Multi-Agent AI Platform API",
                Version = "v1",
                Description = "REST API for the enterprise multi-agent AI platform (chat, agents, RAG, SQL, memory, tools)."
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme."
            };
            document.Components.SecuritySchemes["ApiKey"] = new OpenApiSecurityScheme
            {
                Name = "X-Api-Key",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Machine-to-machine API key."
            };

            return Task.CompletedTask;
        });
    });

    var app = builder.Build();

    // Apply migrations at startup when enabled and a database is reachable.
    ApplyMigrations(app);

    app.UseSerilogRequestLogging();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // Serve the OpenAPI document and the Scalar API reference UI.
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Enterprise Multi-Agent AI Platform API")
            .WithTheme(ScalarTheme.Purple)
            .WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    // Redirect the site root to the Scalar API reference.
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();

    app.MapControllers();
    app.MapHub<ChatHub>("/hubs/chat");
    app.MapHealthChecks("/health");

    Log.Information("Enterprise Multi-Agent AI Platform API starting.");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup.");
}
finally
{
    Log.CloseAndFlush();
}

static void ApplyMigrations(WebApplication app)
{
    var options = app.Configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>()
                  ?? new PersistenceOptions();
    if (!options.AutoMigrate)
    {
        return;
    }

    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AgentDbContext>();
        if (db.Database.CanConnect() || !string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            db.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Database migration skipped (database unavailable).");
    }
}

/// <summary>Exposed for the integration test host (WebApplicationFactory&lt;Program&gt;).</summary>
public partial class Program;
