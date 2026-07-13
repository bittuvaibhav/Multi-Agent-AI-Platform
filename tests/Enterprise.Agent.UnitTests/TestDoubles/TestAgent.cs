using Enterprise.Agent.Core.Abstractions.Agents;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Enums;

namespace Enterprise.Agent.UnitTests.TestDoubles;

/// <summary>A configurable in-memory agent for orchestrator/registry/planner tests.</summary>
public sealed class TestAgent : IAgent
{
    private readonly Func<AgentRequest, CancellationToken, Task<AgentResponse>> _behavior;

    public TestAgent(AgentDescriptor descriptor, Func<AgentRequest, CancellationToken, Task<AgentResponse>> behavior)
    {
        Descriptor = descriptor;
        _behavior = behavior;
    }

    public int InvocationCount { get; private set; }

    public AgentDescriptor Descriptor { get; }

    public Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        InvocationCount++;
        return _behavior(request, cancellationToken);
    }

    public static AgentDescriptor Descriptor2(string name, AgentRole role, params string[] keywords) => new()
    {
        Name = name,
        Role = role,
        Description = $"Test agent {name}.",
        Keywords = keywords
    };

    public static TestAgent Succeeding(string name, string output, AgentRole role = AgentRole.Research, params string[] keywords) =>
        new(Descriptor2(name, role, keywords), (_, _) => Task.FromResult(AgentResponse.Ok(name, output)));

    public static TestAgent Failing(string name, string error, AgentRole role = AgentRole.Research) =>
        new(Descriptor2(name, role), (_, _) => Task.FromResult(AgentResponse.Fail(name, error)));
}
