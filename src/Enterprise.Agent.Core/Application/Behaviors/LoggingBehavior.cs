using System.Diagnostics;
using Enterprise.Agent.Shared.Correlation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Agent.Core.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs the start, completion and duration of every request,
/// stamped with the ambient correlation id.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICorrelationContext _correlation;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger, ICorrelationContext correlation)
    {
        _logger = logger;
        _correlation = correlation;
    }

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "[{CorrelationId}] Handling {Request}", _correlation.CorrelationId, requestName);

        try
        {
            var response = await next().ConfigureAwait(false);
            stopwatch.Stop();
            _logger.LogInformation(
                "[{CorrelationId}] Handled {Request} in {Ms}ms",
                _correlation.CorrelationId, requestName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex, "[{CorrelationId}] {Request} failed after {Ms}ms",
                _correlation.CorrelationId, requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
