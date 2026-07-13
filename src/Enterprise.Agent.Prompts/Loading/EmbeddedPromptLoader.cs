using System.Reflection;
using Enterprise.Agent.Models.Prompts;
using Enterprise.Agent.Prompts.Registry;

namespace Enterprise.Agent.Prompts.Loading;

/// <summary>
/// Loads prompt templates from embedded <c>*.prompt.md</c> resources shipped in this
/// assembly. Each file uses a small YAML-style front matter block:
/// <code>
/// ---
/// name: planner
/// version: v1
/// description: Decomposes a goal into agent steps.
/// ---
/// &lt;template body with {{tokens}}&gt;
/// </code>
/// </summary>
public static class EmbeddedPromptLoader
{
    private const string ResourceSuffix = ".prompt.md";

    /// <summary>Reads every embedded prompt resource and registers it in <paramref name="registry"/>.</summary>
    public static int LoadInto(PromptRegistry registry, Assembly? assembly = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        assembly ??= typeof(EmbeddedPromptLoader).Assembly;

        var loaded = 0;
        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                continue;
            }

            using var reader = new StreamReader(stream);
            var raw = reader.ReadToEnd();
            if (TryParse(raw, out var template) && template is not null)
            {
                registry.Register(template);
                loaded++;
            }
        }

        return loaded;
    }

    internal static bool TryParse(string raw, out PromptTemplate? template)
    {
        template = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var text = raw.Replace("\r\n", "\n");
        if (!text.StartsWith("---\n", StringComparison.Ordinal))
        {
            return false;
        }

        var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (end < 0)
        {
            return false;
        }

        var frontMatter = text.Substring(4, end - 4);
        var body = text[(end + 4)..].TrimStart('\n');

        string? name = null, version = null, description = null;
        foreach (var line in frontMatter.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = line.IndexOf(':');
            if (idx <= 0)
            {
                continue;
            }

            var key = line[..idx].Trim().ToLowerInvariant();
            var value = line[(idx + 1)..].Trim();
            switch (key)
            {
                case "name": name = value; break;
                case "version": version = value; break;
                case "description": description = value; break;
            }
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        template = new PromptTemplate
        {
            Name = name,
            Version = version,
            Template = body.Trim(),
            Description = description
        };
        return true;
    }
}
