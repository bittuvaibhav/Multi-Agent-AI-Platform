using Enterprise.Agent.Models.Tools;
using Microsoft.SemanticKernel;

namespace Enterprise.Agent.Core.Abstractions.Tools;

/// <summary>
/// A source of Semantic Kernel plugins. Each tool package contributes one or more
/// <see cref="KernelPlugin"/> instances that are imported into agent kernels.
/// </summary>
public interface IKernelPluginSource
{
    KernelPlugin BuildPlugin();
}

/// <summary>Catalogues the available tools/plugins and can invoke them directly.</summary>
public interface IToolRegistry
{
    IReadOnlyCollection<ToolDescriptor> Descriptors { get; }

    IReadOnlyCollection<KernelPlugin> Plugins { get; }

    Task<ToolResult> InvokeAsync(ToolInvocation invocation, CancellationToken cancellationToken = default);
}
