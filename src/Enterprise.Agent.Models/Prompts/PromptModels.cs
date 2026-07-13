namespace Enterprise.Agent.Models.Prompts;

/// <summary>A versioned, named prompt template with simple {{token}} placeholders.</summary>
public sealed record PromptTemplate
{
    public required string Name { get; init; }

    public required string Version { get; init; }

    public required string Template { get; init; }

    public string? Description { get; init; }

    /// <summary>Renders the template by substituting <c>{{key}}</c> tokens.</summary>
    public string Render(IReadOnlyDictionary<string, string> values)
    {
        var result = Template;
        foreach (var (key, value) in values)
        {
            result = result.Replace($"{{{{{key}}}}}", value, StringComparison.Ordinal);
        }

        return result;
    }
}
