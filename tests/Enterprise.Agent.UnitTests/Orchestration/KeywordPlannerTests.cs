using Enterprise.Agent.Core.Agents;
using Enterprise.Agent.Core.Options;
using Enterprise.Agent.Core.Orchestration;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.UnitTests.TestDoubles;
using Microsoft.Extensions.Options;
using Xunit;

namespace Enterprise.Agent.UnitTests.Orchestration;

public sealed class KeywordPlannerTests
{
    private static KeywordPlanner Build()
    {
        var registry = new AgentRegistry(
        [
            TestAgent.Succeeding("SqlAgent", "sql", AgentRole.Sql, "sql", "database"),
            TestAgent.Succeeding("WriterAgent", "written", AgentRole.Writer, "write", "draft"),
            TestAgent.Succeeding("ResearchAgent", "researched", AgentRole.Research, "research")
        ]);
        return new KeywordPlanner(registry, Options.Create(new PlannerOptions { FallbackAgent = "ResearchAgent" }));
    }

    [Fact]
    public async Task Plan_SelectsKeywordMatchedAgents()
    {
        var plan = await Build().CreatePlanAsync("please write a draft about our database");
        Assert.Contains(plan.Steps, s => s.AgentName == "WriterAgent");
        Assert.Contains(plan.Steps, s => s.AgentName == "SqlAgent");
    }

    [Fact]
    public async Task Plan_NoMatch_FallsBackToDefaultAgent()
    {
        var plan = await Build().CreatePlanAsync("xyzzy unrelated goal");
        Assert.Single(plan.Steps);
        Assert.Equal("ResearchAgent", plan.Steps[0].AgentName);
    }
}
