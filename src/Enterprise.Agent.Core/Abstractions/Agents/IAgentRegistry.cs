using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Enums;

namespace Enterprise.Agent.Core.Abstractions.Agents;

/// <summary>Resolves agents by name or role and exposes their descriptors for planning.</summary>
public interface IAgentRegistry
{
    IReadOnlyCollection<IAgent> All { get; }

    IReadOnlyCollection<AgentDescriptor> Descriptors { get; }

    bool TryGet(string name, out IAgent? agent);

    IAgent Get(string name);

    IAgent? FindByRole(AgentRole role);
}
