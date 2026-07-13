using System.Text;
using System.Text.Json;
using Enterprise.Agent.Core.Abstractions.AI;
using Enterprise.Agent.Core.Abstractions.Agents;
using Enterprise.Agent.Core.Abstractions.Orchestration;
using Enterprise.Agent.Core.Options;
using Enterprise.Agent.Models.Chat;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Models.Orchestration;
using Enterprise.Agent.Prompts.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Agent.Core.Orchestration;

/// <summary>
/// LLM-backed planner. Renders the planner prompt with the catalogue of available agents,
/// asks the model to emit a JSON plan, and parses it. Any failure (model error, malformed
/// JSON, unknown agent) degrades gracefully to the deterministic <see cref="KeywordPlanner"/>.
/// </summary>
public sealed class LlmPlanner : IPlanner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IChatCompletionProvider _chat;
    private readonly IAgentRegistry _registry;
    private readonly IPromptProvider _prompts;
    private readonly KeywordPlanner _fallback;
    private readonly PlannerOptions _options;
    private readonly ILogger<LlmPlanner> _logger;

    public LlmPlanner(
        IChatCompletionProvider chat,
        IAgentRegistry registry,
        IPromptProvider prompts,
        KeywordPlanner fallback,
        IOptions<PlannerOptions> options,
        ILogger<LlmPlanner> logger)
    {
        _chat = chat;
        _registry = registry;
        _prompts = prompts;
        _fallback = fallback;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<WorkflowPlan> CreatePlanAsync(string goal, CancellationToken cancellationToken = default)
    {
        if (!_options.UseLlmPlanner)
        {
            return await _fallback.CreatePlanAsync(goal, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var prompt = _prompts.Render(
                _options.PromptName,
                new Dictionary<string, string>
                {
                    ["agents"] = DescribeAgents(),
                    ["goal"] = goal ?? string.Empty
                },
                _options.PromptVersion);

            var messages = new List<ChatMessage>
            {
                new() { Role = MessageRole.User, Content = prompt }
            };

            var raw = await _chat.CompleteAsync(messages, new ChatCompletionSettings { Temperature = 0.0 }, cancellationToken)
                .ConfigureAwait(false);

            var plan = TryParsePlan(goal ?? string.Empty, raw);
            if (plan is { IsEmpty: false })
            {
                return plan;
            }

            _logger.LogWarning("LLM planner produced no usable steps; falling back to keyword planner.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM planner failed; falling back to keyword planner.");
        }

        return await _fallback.CreatePlanAsync(goal ?? string.Empty, cancellationToken).ConfigureAwait(false);
    }

    private string DescribeAgents()
    {
        var builder = new StringBuilder();
        foreach (var d in _registry.Descriptors)
        {
            builder.Append("- ").Append(d.Name).Append(" — ").Append(d.Description).Append('\n');
        }

        return builder.ToString();
    }

    private WorkflowPlan? TryParsePlan(string goal, string raw)
    {
        var json = ExtractJsonObject(raw);
        if (json is null)
        {
            return null;
        }

        PlanDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<PlanDto>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (dto?.Steps is null || dto.Steps.Count == 0)
        {
            return null;
        }

        var steps = new List<PlannedStep>();
        var order = 1;
        foreach (var step in dto.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Agent) || !_registry.TryGet(step.Agent!, out _))
            {
                continue;
            }

            steps.Add(new PlannedStep
            {
                AgentName = step.Agent!,
                Instruction = string.IsNullOrWhiteSpace(step.Instruction) ? goal : step.Instruction!,
                Order = step.Order > 0 ? step.Order : order,
                DependsOn = step.DependsOn ?? []
            });
            order++;
        }

        if (steps.Count == 0)
        {
            return null;
        }

        var mode = string.Equals(dto.Mode, "Parallel", StringComparison.OrdinalIgnoreCase)
            ? WorkflowMode.Parallel
            : WorkflowMode.Sequential;

        return new WorkflowPlan
        {
            Goal = goal,
            Mode = mode,
            Steps = steps,
            Rationale = dto.Rationale
        };
    }

    private static string? ExtractJsonObject(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        return start >= 0 && end > start ? raw[start..(end + 1)] : null;
    }

    private sealed class PlanDto
    {
        public string? Mode { get; set; }
        public string? Rationale { get; set; }
        public List<StepDto>? Steps { get; set; }
    }

    private sealed class StepDto
    {
        public string? Agent { get; set; }
        public string? Instruction { get; set; }
        public int Order { get; set; }
        public List<string>? DependsOn { get; set; }
    }
}
