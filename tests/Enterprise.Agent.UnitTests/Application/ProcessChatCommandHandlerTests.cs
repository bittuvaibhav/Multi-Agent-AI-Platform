using Enterprise.Agent.Core.Abstractions.Orchestration;
using Enterprise.Agent.Core.Agents;
using Enterprise.Agent.Core.Application.Chat;
using Enterprise.Agent.Models.Chat;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Models.Orchestration;
using Enterprise.Agent.Shared.Correlation;
using Enterprise.Agent.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enterprise.Agent.UnitTests.Application;

public sealed class ProcessChatCommandHandlerTests
{
    private sealed class NoopOrchestrator : IAgentOrchestrator
    {
        public bool RanGoal { get; private set; }

        public Task<ExecutionHistory> RunGoalAsync(AgentExecutionContext context, CancellationToken cancellationToken = default)
        {
            RanGoal = true;
            return Task.FromResult(new ExecutionHistory
            {
                CorrelationId = context.CorrelationId,
                Goal = context.Goal,
                Mode = WorkflowMode.Sequential,
                Status = ExecutionStatus.Succeeded,
                Steps = [],
                FinalOutput = "orchestrated answer"
            });
        }

        public Task<ExecutionHistory> ExecuteAsync(WorkflowPlan plan, AgentExecutionContext context, CancellationToken cancellationToken = default)
            => RunGoalAsync(context, cancellationToken);
    }

    [Fact]
    public async Task PreferredAgent_IsInvokedDirectly_BypassingOrchestrator()
    {
        var registry = new AgentRegistry([TestAgent.Succeeding("WriterAgent", "drafted", AgentRole.Writer)]);
        var orchestrator = new NoopOrchestrator();
        var handler = new ProcessChatCommandHandler(
            orchestrator, registry, new CorrelationContext(), NullLogger<ProcessChatCommandHandler>.Instance);

        var response = await handler.Handle(
            new ProcessChatCommand(new ChatRequest { Message = "hi", PreferredAgent = "WriterAgent" }),
            CancellationToken.None);

        Assert.Equal("drafted", response.Answer);
        Assert.Contains("WriterAgent", response.AgentsInvoked);
        Assert.False(orchestrator.RanGoal);
    }

    [Fact]
    public async Task NoPreferredAgent_UsesOrchestrator()
    {
        var registry = new AgentRegistry([TestAgent.Succeeding("WriterAgent", "drafted", AgentRole.Writer)]);
        var orchestrator = new NoopOrchestrator();
        var handler = new ProcessChatCommandHandler(
            orchestrator, registry, new CorrelationContext(), NullLogger<ProcessChatCommandHandler>.Instance);

        var response = await handler.Handle(
            new ProcessChatCommand(new ChatRequest { Message = "do something" }), CancellationToken.None);

        Assert.True(orchestrator.RanGoal);
        Assert.Equal("orchestrated answer", response.Answer);
    }
}
