namespace Enterprise.Agent.Models.Tools;

/// <summary>Describes a tool/plugin function available to agents.</summary>
public sealed record ToolDescriptor
{
    public required string PluginName { get; init; }

    public required string FunctionName { get; init; }

    public required string Description { get; init; }

    public IReadOnlyList<ToolParameter> Parameters { get; init; } = [];

    public string FullyQualifiedName => $"{PluginName}.{FunctionName}";
}

/// <summary>A single parameter of a tool function.</summary>
public sealed record ToolParameter
{
    public required string Name { get; init; }

    public required string Type { get; init; }

    public string Description { get; init; } = string.Empty;

    public bool IsRequired { get; init; }
}

/// <summary>A request to invoke a tool by name with string arguments.</summary>
public sealed record ToolInvocation
{
    public required string PluginName { get; init; }

    public required string FunctionName { get; init; }

    public IReadOnlyDictionary<string, string> Arguments { get; init; } =
        new Dictionary<string, string>();
}

/// <summary>Result of a tool invocation.</summary>
public sealed record ToolResult
{
    public required bool Success { get; init; }

    public string Output { get; init; } = string.Empty;

    public string? Error { get; init; }
}
