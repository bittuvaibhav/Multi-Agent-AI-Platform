using Enterprise.Agent.Models.Prompts;
using Enterprise.Agent.Prompts;
using Enterprise.Agent.Prompts.Registry;
using Xunit;

namespace Enterprise.Agent.UnitTests.Prompts;

public sealed class PromptRegistryTests
{
    [Fact]
    public void Register_And_Get_ReturnsLatestVersion()
    {
        var registry = new PromptRegistry();
        registry.Register(new PromptTemplate { Name = "p", Version = "v1", Template = "one" });
        registry.Register(new PromptTemplate { Name = "p", Version = "v2", Template = "two" });

        Assert.Equal("two", registry.Get("p").Template);
        Assert.Equal("one", registry.Get("p", "v1").Template);
    }

    [Fact]
    public void Render_SubstitutesTokens()
    {
        var registry = new PromptRegistry();
        registry.Register(new PromptTemplate { Name = "greet", Version = "v1", Template = "Hello {{name}}!" });

        Assert.Equal("Hello World!", registry.Render("greet", new Dictionary<string, string> { ["name"] = "World" }));
    }

    [Fact]
    public void BuildRegistry_LoadsDefaultsAndEmbedded()
    {
        var registry = DependencyInjection.BuildRegistry();

        Assert.Contains(registry.All, t => t.Name == "planner");
        Assert.Contains(registry.All, t => t.Name == "sql-generate");
        // planner v2 is supplied as an embedded prompt file and should be the latest.
        Assert.Equal("v2", registry.Get("planner").Version);
    }
}
