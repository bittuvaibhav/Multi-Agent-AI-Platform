using System.Collections.Concurrent;
using Enterprise.Agent.Core.Abstractions.Agents;
using Enterprise.Agent.Core.Abstractions.Orchestration;
using Enterprise.Agent.Core.Options;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Models.Orchestration;
using Enterprise.Agent.Shared.Time;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Agent.Core.Orchestration;

/// <summary>
/// Default orchestrator. Executes a <see cref="WorkflowPlan"/> either sequentially
/// (each step sees the accumulated context of prior steps) or in parallel, applying
/// per-step retry and timeout policies and honouring cancellation throughout. Produces
/// a complete <see cref="ExecutionHistory"/> for auditing.
/// </summary>
public sealed class AgentOrchestrator : IAgentOrchestrator
{
    private readonly IAgentRegistry _registry;
    private readonly IPlanner _planner;
    private readonly OrchestratorOptions _options;
    private readonly IClock _clock;
    private readonly ILogger<AgentOrchestrator> _logger;

    public AgentOrchestrator(
        IAgentRegistry registry,
        IPlanner planner,
        IOptions<OrchestratorOptions> options,
        IClock clock,
        ILogger<AgentOrchestrator> logger)
    {
        _registry = registry;
        _planner = planner;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ExecutionHistory> RunGoalAsync(
        AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var plan = await _planner.CreatePlanAsync(context.Goal, cancellationToken).ConfigureAwait(false);
        return await ExecuteAsync(plan, context, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ExecutionHistory> ExecuteAsync(
        WorkflowPlan plan, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        var startedAt = _clock.UtcNow;
        _logger.LogInformation(
            "Orchestration {CorrelationId} started: {StepCount} step(s), mode {Mode}.",
            context.CorrelationId, plan.Steps.Count, plan.Mode);

        var steps = plan.Mode == WorkflowMode.Parallel
            ? await ExecuteParallelAsync(plan, context, cancellationToken).ConfigureAwait(false)
            : await ExecuteSequentialAsync(plan, context, cancellationToken).ConfigureAwait(false);

        var overall = DetermineOverallStatus(steps);
        var finalOutput = BuildFinalOutput(steps, plan.Mode);

        var completedAt = _clock.UtcNow;
        _logger.LogInformation(
            "Orchestration {CorrelationId} finished with status {Status} in {Ms}ms.",
            context.CorrelationId, overall, (completedAt - startedAt).TotalMilliseconds);

        return new ExecutionHistory
        {
            CorrelationId = context.CorrelationId,
            Goal = context.Goal,
            Mode = plan.Mode,
            Status = overall,
            Steps = steps,
            FinalOutput = finalOutput,
            StartedAt = startedAt,
            CompletedAt = completedAt
        };
    }

    private async Task<List<ExecutionStep>> ExecuteSequentialAsync(
        WorkflowPlan plan, AgentExecutionContext context, CancellationToken cancellationToken)
    {
        var steps = new List<ExecutionStep>(plan.Steps.Count);
        foreach (var planned in plan.Steps.OrderBy(s => s.Order))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request = BuildRequest(planned, context, context.BuildAggregatedContext());
            var step = await RunStepAsync(planned, request, cancellationToken).ConfigureAwait(false);
            steps.Add(step);

            if (step.Status == ExecutionStatus.Succeeded)
            {
                context.RecordStepOutput(planned.AgentName, AgentResponse.Ok(planned.AgentName, step.Output ?? string.Empty));
            }
            else if (_options.StopOnFirstFailure)
            {
                _logger.LogWarning(
                    "Step {Agent} failed ({Status}); aborting remaining sequential steps.",
                    planned.AgentName, step.Status);
                break;
            }
        }

        return steps;
    }

    private async Task<List<ExecutionStep>> ExecuteParallelAsync(
        WorkflowPlan plan, AgentExecutionContext context, CancellationToken cancellationToken)
    {
        using var gate = new SemaphoreSlim(Math.Max(1, _options.MaxDegreeOfParallelism));
        var results = new ConcurrentBag<ExecutionStep>();
        var initialContext = context.BuildAggregatedContext();

        var tasks = plan.Steps.Select(async planned =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var request = BuildRequest(planned, context, initialContext);
                var step = await RunStepAsync(planned, request, cancellationToken).ConfigureAwait(false);
                results.Add(step);
                if (step.Status == ExecutionStatus.Succeeded)
                {
                    context.RecordStepOutput(
                        planned.AgentName, AgentResponse.Ok(planned.AgentName, step.Output ?? string.Empty));
                }
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.OrderBy(s => s.StartedAt).ToList();
    }

    /// <summary>Executes one step with retry + timeout policies.</summary>
    private async Task<ExecutionStep> RunStepAsync(
        PlannedStep planned, AgentRequest request, CancellationToken cancellationToken)
    {
        var startedAt = _clock.UtcNow;

        if (!_registry.TryGet(planned.AgentName, out var agent) || agent is null)
        {
            return new ExecutionStep
            {
                AgentName = planned.AgentName,
                Instruction = planned.Instruction,
                Status = ExecutionStatus.Failed,
                Error = $"Agent '{planned.AgentName}' is not registered.",
                Attempts = 0,
                StartedAt = startedAt,
                CompletedAt = _clock.UtcNow
            };
        }

        var attempts = 0;
        AgentResponse? response = null;
        var status = ExecutionStatus.Failed;
        string? error = null;

        for (var attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            attempts = attempt + 1;
            cancellationToken.ThrowIfCancellationRequested();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_options.StepTimeout);

            try
            {
                response = await agent.ExecuteAsync(request, timeoutCts.Token).ConfigureAwait(false);
                if (response.Success)
                {
                    status = ExecutionStatus.Succeeded;
                    break;
                }

                error = response.ErrorMessage;
                status = ExecutionStatus.Failed;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                status = ExecutionStatus.Cancelled;
                error = "Execution cancelled by caller.";
                throw;
            }
            catch (OperationCanceledException)
            {
                // Timeout (linked token fired, outer token not cancelled).
                status = ExecutionStatus.TimedOut;
                error = $"Step timed out after {_options.StepTimeout.TotalSeconds:0.##}s.";
                _logger.LogWarning("Step {Agent} timed out on attempt {Attempt}.", planned.AgentName, attempts);
            }
            catch (Exception ex)
            {
                status = ExecutionStatus.Failed;
                error = ex.Message;
                _logger.LogWarning(ex, "Step {Agent} threw on attempt {Attempt}.", planned.AgentName, attempts);
            }

            if (attempt < _options.MaxRetries)
            {
                var delay = _options.RetryDelay * (attempt + 1);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        return new ExecutionStep
        {
            AgentName = planned.AgentName,
            Instruction = planned.Instruction,
            Status = status,
            Output = response is { Success: true } ? response.Output : null,
            Error = status == ExecutionStatus.Succeeded ? null : error,
            Attempts = attempts,
            StartedAt = startedAt,
            CompletedAt = _clock.UtcNow
        };
    }

    private static AgentRequest BuildRequest(
        PlannedStep planned, AgentExecutionContext context, string aggregatedContext) => new()
        {
            Input = planned.Instruction,
            Context = string.IsNullOrWhiteSpace(aggregatedContext) ? null : aggregatedContext,
            ConversationId = context.ConversationId,
            UserId = context.UserId,
            TenantId = context.TenantId,
            Parameters = planned.Parameters
        };

    private static ExecutionStatus DetermineOverallStatus(IReadOnlyList<ExecutionStep> steps)
    {
        if (steps.Count == 0)
        {
            return ExecutionStatus.Skipped;
        }

        if (steps.Any(s => s.Status == ExecutionStatus.Cancelled))
        {
            return ExecutionStatus.Cancelled;
        }

        return steps.All(s => s.Status == ExecutionStatus.Succeeded)
            ? ExecutionStatus.Succeeded
            : steps.Any(s => s.Status == ExecutionStatus.Succeeded)
                ? ExecutionStatus.Succeeded // partial success still yields output
                : ExecutionStatus.Failed;
    }

    private static string BuildFinalOutput(IReadOnlyList<ExecutionStep> steps, WorkflowMode mode)
    {
        var successful = steps.Where(s => s.Status == ExecutionStatus.Succeeded && !string.IsNullOrWhiteSpace(s.Output))
            .ToList();

        if (successful.Count == 0)
        {
            return string.Empty;
        }

        if (mode == WorkflowMode.Sequential)
        {
            return successful[^1].Output ?? string.Empty;
        }

        return string.Join("\n\n", successful.Select(s => $"### {s.AgentName}\n{s.Output}"));
    }
}
