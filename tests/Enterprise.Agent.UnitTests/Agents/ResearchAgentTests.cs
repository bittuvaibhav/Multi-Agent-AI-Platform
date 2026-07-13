using Enterprise.Agent.Core.Agents.Implementations;
using Enterprise.Agent.Core.Options;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Prompts;
using Enterprise.Agent.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enterprise.Agent.UnitTests.Agents;

public sealed class ResearchAgentTests
{
    [Fact]
    public async Task Execute_ReturnsModelOutput_ViaSemanticKernel()
    {
        var agent = new ResearchAgent(
            new FakeKernelFactory("Here is the briefing you requested."),
            DependencyInjection.BuildRegistry(),
            new AgentDefaults(),
            NullLogger<ResearchAgent>.Instance);

        var response = await agent.ExecuteAsync(new AgentRequest { Input = "Tell me about widgets." });

        Assert.True(response.Success);
        Assert.Equal("Here is the briefing you requested.", response.Output);
        Assert.Equal("ResearchAgent", response.AgentName);
    }

    [Fact]
    public void Descriptor_AdvertisesResearchKeywords()
    {
        var agent = new ResearchAgent(
            new FakeKernelFactory("x"), DependencyInjection.BuildRegistry(),
            new AgentDefaults(), NullLogger<ResearchAgent>.Instance);

        Assert.Contains("research", agent.Descriptor.Keywords);
    }
}
