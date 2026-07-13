using Microsoft.SemanticKernel;

namespace Enterprise.Agent.Core.Abstractions.AI;

/// <summary>
/// Builds configured Semantic Kernel <see cref="Kernel"/> instances. Every agent obtains
/// its kernel from this factory, which wires the configured chat/embedding provider and
/// registers the platform's tool plugins.
/// </summary>
public interface IKernelFactory
{
    /// <summary>
    /// Creates a kernel using the given provider (or the configured default when null),
    /// optionally importing the registered tool plugins for tool-calling scenarios.
    /// </summary>
    Kernel Create(string? providerId = null, bool importPlugins = true);
}
