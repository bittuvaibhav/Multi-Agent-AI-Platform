using Enterprise.Agent.Core.Abstractions.Agents;
using Enterprise.Agent.Core.Abstractions.Orchestration;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Chat;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Models.Orchestration;
using Enterprise.Agent.Shared.Correlation;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Agent.Core.Application.Chat;

/// <summary>Processes a chat turn: plans and runs the appropriate agent(s) for the message.</summary>
public sealed record ProcessChatCommand(ChatRequest Request) : IRequest<ChatResponse>;

public sealed class ProcessChatCommandValidator : AbstractValidator<ProcessChatCommand>
{
    public ProcessChatCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x.Request.Message)
            .NotEmpty().WithMessage("Message must not be empty.")
            .MaximumLength(16_000);
    }
}

public sealed class ProcessChatCommandHandler : IRequestHandler<ProcessChatCommand, ChatResponse>
{
    private readonly IAgentOrchestrator _orchestrator;
    private readonly IAgentRegistry _registry;
    private readonly ICorrelationContext _correlation;
    private readonly ILogger<ProcessChatCommandHandler> _logger;

    public ProcessChatCommandHandler(
        IAgentOrchestrator orchestrator,
        IAgentRegistry registry,
        ICorrelationContext correlation,
        ILogger<ProcessChatCommandHandler> logger)
    {
        _orchestrator = orchestrator;
        _registry = registry;
        _correlation = correlation;
        _logger = logger;
    }

    public async Task<ChatResponse> Handle(ProcessChatCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var conversationId = string.IsNullOrWhiteSpace(request.ConversationId)
            ? Guid.NewGuid().ToString("N")
            : request.ConversationId!;

        var context = new AgentExecutionContext(
            _correlation.CorrelationId, request.Message, conversationId);

        // A caller may force a specific agent, bypassing the planner.
        if (!string.IsNullOrWhiteSpace(request.PreferredAgent)
            && _registry.TryGet(request.PreferredAgent!, out var agent) && agent is not null)
        {
            _logger.LogInformation("Routing chat directly to preferred agent {Agent}.", agent.Descriptor.Name);
            var response = await agent.ExecuteAsync(
                new AgentRequest { Input = request.Message, ConversationId = conversationId },
                cancellationToken).ConfigureAwait(false);

            return new ChatResponse
            {
                ConversationId = conversationId,
                Answer = response.Success ? response.Output : response.ErrorMessage ?? "The agent failed.",
                CorrelationId = _correlation.CorrelationId,
                AgentsInvoked = [agent.Descriptor.Name]
            };
        }

        var history = await _orchestrator.RunGoalAsync(context, cancellationToken).ConfigureAwait(false);

        return new ChatResponse
        {
            ConversationId = conversationId,
            Answer = string.IsNullOrWhiteSpace(history.FinalOutput)
                ? "No agent was able to produce a response."
                : history.FinalOutput,
            CorrelationId = history.CorrelationId,
            AgentsInvoked = history.Steps
                .Where(s => s.Status == ExecutionStatus.Succeeded)
                .Select(s => s.AgentName)
                .Distinct()
                .ToArray()
        };
    }
}
