using System.Text.Json;
using Enterprise.Agent.Shared.Correlation;
using Enterprise.Agent.Shared.Exceptions;
using Enterprise.Agent.Shared.Results;

namespace Enterprise.Agent.Api.Middleware;

/// <summary>
/// Converts unhandled exceptions into RFC 7807 problem-details responses, mapping domain
/// exceptions to appropriate status codes and including the correlation id.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    // 499 (nginx) represents a client-cancelled request.
    private const int ClientClosedRequest = 499;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationContext correlation)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing {Path}.", context.Request.Path);
            await WriteProblemAsync(context, ex, correlation.CorrelationId).ConfigureAwait(false);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, Exception ex, string correlationId)
    {
        var (status, title, detail, errors) = Map(ex);

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var payload = new Dictionary<string, object?>
        {
            ["type"] = $"https://httpstatuses.io/{status}",
            ["title"] = title,
            ["status"] = status,
            ["detail"] = detail,
            ["correlationId"] = correlationId
        };
        if (errors is not null)
        {
            payload["errors"] = errors;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions)).ConfigureAwait(false);
    }

    private static (int Status, string Title, string Detail, IReadOnlyDictionary<string, string[]>? Errors) Map(Exception ex) =>
        ex switch
        {
            ValidationException v => (StatusCodes.Status400BadRequest, "Validation failed", v.Message, v.Failures),
            NotFoundException n => (StatusCodes.Status404NotFound, "Resource not found", n.Message, null),
            EnterpriseAgentException e when e.Error.Type == ErrorType.Unauthorized =>
                (StatusCodes.Status401Unauthorized, "Unauthorized", e.Message, null),
            EnterpriseAgentException e when e.Error.Type == ErrorType.Forbidden =>
                (StatusCodes.Status403Forbidden, "Forbidden", e.Message, null),
            EnterpriseAgentException e when e.Error.Type == ErrorType.Timeout =>
                (StatusCodes.Status504GatewayTimeout, "Timed out", e.Message, null),
            EnterpriseAgentException e => (StatusCodes.Status400BadRequest, "Request failed", e.Message, null),
            OperationCanceledException => (ClientClosedRequest, "Request cancelled", "The request was cancelled.", null),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error",
                "An unexpected error occurred. See logs for details.", null)
        };
}

