using Enterprise.Agent.Core.Abstractions.Tools;
using Enterprise.Agent.Models.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Enterprise.Agent.Plugins;

/// <summary>
/// Aggregates all registered <see cref="IKernelPluginSource"/> instances into a catalogue of
/// Semantic Kernel plugins, exposes their metadata as <see cref="ToolDescriptor"/>s and can
/// invoke any tool function directly (used by the /tools API).
/// </summary>
public sealed class ToolRegistry : IToolRegistry
{
    private readonly Lazy<IReadOnlyList<KernelPlugin>> _plugins;
    private readonly Lazy<IReadOnlyList<ToolDescriptor>> _descriptors;
    private readonly Kernel _invocationKernel;
    private readonly ILogger<ToolRegistry> _logger;

    public ToolRegistry(IEnumerable<IKernelPluginSource> sources, ILogger<ToolRegistry> logger)
    {
        ArgumentNullException.ThrowIfNull(sources);
        _logger = logger;
        var materialised = sources.ToArray();

        _plugins = new Lazy<IReadOnlyList<KernelPlugin>>(() =>
            materialised.Select(s => s.BuildPlugin()).ToArray());

        _descriptors = new Lazy<IReadOnlyList<ToolDescriptor>>(BuildDescriptors);

        // A bare kernel is sufficient to invoke native (non-LLM) tool functions.
        _invocationKernel = Kernel.CreateBuilder().Build();
    }

    public IReadOnlyCollection<KernelPlugin> Plugins => _plugins.Value.ToArray();

    public IReadOnlyCollection<ToolDescriptor> Descriptors => _descriptors.Value.ToArray();

    public async Task<ToolResult> InvokeAsync(
        ToolInvocation invocation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        var plugin = _plugins.Value.FirstOrDefault(
            p => string.Equals(p.Name, invocation.PluginName, StringComparison.OrdinalIgnoreCase));
        if (plugin is null)
        {
            return new ToolResult { Success = false, Error = $"Plugin '{invocation.PluginName}' not found." };
        }

        if (!plugin.TryGetFunction(invocation.FunctionName, out var function))
        {
            return new ToolResult
            {
                Success = false,
                Error = $"Function '{invocation.FunctionName}' not found in plugin '{invocation.PluginName}'."
            };
        }

        try
        {
            var arguments = new KernelArguments();
            foreach (var (key, value) in invocation.Arguments)
            {
                arguments[key] = value;
            }

            var result = await function.InvokeAsync(_invocationKernel, arguments, cancellationToken)
                .ConfigureAwait(false);

            return new ToolResult { Success = true, Output = result.GetValue<object>()?.ToString() ?? string.Empty };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tool invocation {Plugin}.{Function} failed.",
                invocation.PluginName, invocation.FunctionName);
            return new ToolResult { Success = false, Error = ex.Message };
        }
    }

    private IReadOnlyList<ToolDescriptor> BuildDescriptors()
    {
        var descriptors = new List<ToolDescriptor>();
        foreach (var plugin in _plugins.Value)
        {
            foreach (var function in plugin)
            {
                var metadata = function.Metadata;
                descriptors.Add(new ToolDescriptor
                {
                    PluginName = plugin.Name,
                    FunctionName = metadata.Name,
                    Description = metadata.Description ?? string.Empty,
                    Parameters = metadata.Parameters.Select(p => new ToolParameter
                    {
                        Name = p.Name,
                        Type = p.ParameterType?.Name ?? "string",
                        Description = p.Description ?? string.Empty,
                        IsRequired = p.IsRequired
                    }).ToArray()
                });
            }
        }

        return descriptors;
    }
}
