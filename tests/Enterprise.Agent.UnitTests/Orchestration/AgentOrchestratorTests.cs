using Enterprise.Agent.Core.Agents;
using Enterprise.Agent.Core.Options;
using Enterprise.Agent.Core.Orchestration;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Models.Orchestration;
using Enterprise.Agent.Shared.Time;
using Enterprise.Agent.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Enterprise.Agent.UnitTests.Orchestration;

public sealed class AgentOrchestratorTests
{
    private static AgentOrchestrator Build(
        AgentRegistry registry, WorkflowPlan plan, OrchestratorOptions? options = null)
    {
        return new AgentOrchestrator(
            registry,
            new StubPlanner(plan),
            Options.Create(options ?? new OrchestratorOptions()),
            SystemClock.Instance,
            NullLogger<AgentOrchestrator>.Instance);
    }

    private static WorkflowPlan Sequential(params string[] agents) => new()
    {
        Goal = "goal",
        Mode = WorkflowMode.Sequential,
        Steps = agents.Select((a, i) => new PlannedStep { AgentName = a, Instruction = "do", Order = i + 1 }).ToArray()
    };

    [Fact]
    public async Task Sequential_AllSucceed_ProducesFinalOutput()
    {
        var registry = new AgentRegistry(
        [
            TestAgent.Succeeding("A", "alpha"),
            TestAgent.Succeeding("B", "beta")
        ]);

        var history = await Build(registry, Sequential("A", "B"))
            .ExecuteAsync(Sequential("A", "B"), new AgentExecutionContext("cid", "goal"));

        Assert.Equal(ExecutionStatus.Succeeded, history.Status);
        Assert.Equal("beta", history.FinalOutput);
        Assert.Equal(2, history.Steps.Count);
    }

    [Fact]
    public async Task Sequential_StopsOnFirstFailure_WhenConfigured()
    {
        var b = TestAgent.Succeeding("B", "beta");
        var registry = new AgentRegistry([TestAgent.Failing("A", "boom"), b]);

        var plan = Sequential("A", "B");
        var history = await Build(registry, plan, new OrchestratorOptions { MaxRetries = 0, StopOnFirstFailure = true })
            .ExecuteAsync(plan, new AgentExecutionContext("cid", "goal"));

        Assert.Single(history.Steps);
        Assert.Equal(0, b.InvocationCount);
    }

    [Fact]
    public async Task Step_IsRetried_UpToMaxRetries()
    {
        var attempts = 0;
        var flaky = new TestAgent(
            TestAgent.Descriptor2("Flaky", AgentRole.Research),
            (_, _) =>
            {
                attempts++;
                return Task.FromResult(attempts < 3
                    ? AgentResponse.Fail("Flaky", "transient")
                    : AgentResponse.Ok("Flaky", "recovered"));
            });

        var registry = new AgentRegistry([flaky]);
        var plan = Sequential("Flaky");
        var history = await Build(registry, plan, new OrchestratorOptions { MaxRetries = 3, RetryDelay = TimeSpan.Zero })
            .ExecuteAsync(plan, new AgentExecutionContext("cid", "goal"));

        Assert.Equal(ExecutionStatus.Succeeded, history.Status);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task Parallel_RunsAllSteps()
    {
        var registry = new AgentRegistry(
        [
            TestAgent.Succeeding("A", "alpha"),
            TestAgent.Succeeding("B", "beta"),
            TestAgent.Succeeding("C", "gamma")
        ]);

        var plan = new WorkflowPlan
        {
            Goal = "goal",
            Mode = WorkflowMode.Parallel,
            Steps = new[] { "A", "B", "C" }.Select((a, i) => new PlannedStep { AgentName = a, Instruction = "x", Order = i }).ToArray()
        };

        var history = await Build(registry, plan)
            .ExecuteAsync(plan, new AgentExecutionContext("cid", "goal"));

        Assert.Equal(3, history.Steps.Count);
        Assert.All(history.Steps, s => Assert.Equal(ExecutionStatus.Succeeded, s.Status));
    }

    [Fact]
    public async Task UnknownAgent_IsRecordedAsFailure()
    {
        var registry = new AgentRegistry([TestAgent.Succeeding("A", "alpha")]);
        var plan = Sequential("DoesNotExist");
        var history = await Build(registry, plan, new OrchestratorOptions { MaxRetries = 0 })
            .ExecuteAsync(plan, new AgentExecutionContext("cid", "goal"));

        Assert.Equal(ExecutionStatus.Failed, history.Steps[0].Status);
    }
}
