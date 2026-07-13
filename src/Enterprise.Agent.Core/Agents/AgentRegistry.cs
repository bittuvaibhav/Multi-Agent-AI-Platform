using Enterprise.Agent.Core.Abstractions.Agents;
using Enterprise.Agent.Models.Agents;
using Enterprise.Agent.Models.Enums;

namespace Enterprise.Agent.Core.Agents;

/// <summary>
/// Default agent registry. Populated from all <see cref="IAgent"/> instances registered
/// in the DI container. Names are matched case-insensitively.
/// </summary>
public sealed class AgentRegistry : IAgentRegistry
{
    private readonly IReadOnlyDictionary<string, IAgent> _byName;

    public AgentRegistry(IEnumerable<IAgent> agents)
    {
        ArgumentNullException.ThrowIfNull(agents);
        _byName = agents.ToDictionary(a => a.Descriptor.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<IAgent> All => _byName.Values.ToArray();

    public IReadOnlyCollection<AgentDescriptor> Descriptors =>
        _byName.Values.Select(a => a.Descriptor).ToArray();

    public bool TryGet(string name, out IAgent? agent)
    {
        if (!string.IsNullOrWhiteSpace(name) && _byName.TryGetValue(name, out var found))
        {
            agent = found;
            return true;
        }

        agent = null;
        return false;
    }

    public IAgent Get(string name) =>
        TryGet(name, out var agent) && agent is not null
            ? agent
            : throw new KeyNotFoundException($"No agent registered with name '{name}'.");

    public IAgent? FindByRole(AgentRole role) =>
        _byName.Values.FirstOrDefault(a => a.Descriptor.Role == role);
}
