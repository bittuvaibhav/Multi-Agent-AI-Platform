using Enterprise.Agent.Shared.Constants;
using Enterprise.Agent.Shared.Correlation;
using Serilog.Context;

namespace Enterprise.Agent.Api.Middleware;

/// <summary>
/// Assigns/propagates a correlation id for each request: it is read from the inbound
/// <c>X-Correlation-Id</c> header (or generated), stored in the ambient
/// <see cref="ICorrelationContext"/>, pushed to the Serilog log context and echoed back.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ICorrelationContext correlation)
    {
        var correlationId = context.Request.Headers.TryGetValue(PlatformConstants.CorrelationHeader, out var value)
                            && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : Guid.NewGuid().ToString("N");

        correlation.Set(correlationId);
        context.Response.Headers[PlatformConstants.CorrelationHeader] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context).ConfigureAwait(false);
        }
    }
}
