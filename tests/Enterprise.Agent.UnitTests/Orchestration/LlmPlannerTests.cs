using Enterprise.Agent.Core.Agents;
using Enterprise.Agent.Core.Options;
using Enterprise.Agent.Core.Orchestration;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Prompts;
using Enterprise.Agent.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Enterprise.Agent.UnitTests.Orchestration;

public sealed class LlmPlannerTests
{
    private static AgentRegistry Registry() => new(
    [
        TestAgent.Succeeding("ResearchAgent", "r", AgentRole.Research, "research"),
        TestAgent.Succeeding("WriterAgent", "w", AgentRole.Writer, "write")
    ]);

    private static LlmPlanner Build(FakeChatProvider chat)
    {
        var registry = Registry();
        var prompts = DependencyInjection.BuildRegistry();
        var options = Options.Create(new PlannerOptions { UseLlmPlanner = true, FallbackAgent = "ResearchAgent" });
        var fallback = new KeywordPlanner(registry, options);
        return new LlmPlanner(chat, registry, prompts, fallback, options, NullLogger<LlmPlanner>.Instance);
    }

    [Fact]
    public async Task ParsesValidJsonPlan()
    {
        const string json = """
            {"mode":"Sequential","rationale":"do it","steps":[
              {"agent":"ResearchAgent","instruction":"gather","order":1},
              {"agent":"WriterAgent","instruction":"write it up","order":2}]}
            """;
        var plan = await Build(new FakeChatProvider(json)).CreatePlanAsync("write a report");

        Assert.Equal(2, plan.Steps.Count);
        Assert.Equal("ResearchAgent", plan.Steps[0].AgentName);
        Assert.Equal("WriterAgent", plan.Steps[1].AgentName);
    }

    [Fact]
    public async Task InvalidJson_FallsBackToKeywordPlanner()
    {
        var plan = await Build(new FakeChatProvider("not json at all")).CreatePlanAsync("please write something");
        Assert.False(plan.IsEmpty);
        Assert.Contains(plan.Steps, s => s.AgentName == "WriterAgent");
    }

    [Fact]
    public async Task UnknownAgentsInPlan_AreFilteredOut()
    {
        const string json = """{"mode":"Sequential","steps":[{"agent":"GhostAgent","instruction":"x","order":1}]}""";
        var plan = await Build(new FakeChatProvider(json)).CreatePlanAsync("please write something");
        // No valid steps -> falls back; keyword planner should still pick WriterAgent.
        Assert.Contains(plan.Steps, s => s.AgentName == "WriterAgent");
    }
}
