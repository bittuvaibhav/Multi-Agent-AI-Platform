using System.Collections.Concurrent;
using Enterprise.Agent.Models.Prompts;
using Enterprise.Agent.Prompts.Abstractions;

namespace Enterprise.Agent.Prompts.Registry;

/// <summary>
/// Thread-safe in-memory registry of prompt templates keyed by (name, version).
/// The most recently registered version of a name is tracked as its "latest".
/// </summary>
public sealed class PromptRegistry : IPromptProvider
{
    private readonly ConcurrentDictionary<string, PromptTemplate> _byKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _latestVersion = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<PromptTemplate> All => _byKey.Values.ToArray();

    public void Register(PromptTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        var key = BuildKey(template.Name, template.Version);
        _byKey[key] = template;

        // Track latest by ordinal comparison of version strings (v1 < v2 < v10 handled numerically below).
        _latestVersion.AddOrUpdate(
            template.Name,
            template.Version,
            (_, existing) => CompareVersions(template.Version, existing) >= 0 ? template.Version : existing);
    }

    public PromptTemplate Get(string name, string? version = null)
    {
        if (TryGet(name, out var template, version) && template is not null)
        {
            return template;
        }

        throw new KeyNotFoundException(
            $"Prompt '{name}'{(version is null ? string.Empty : $" version '{version}'")} was not found in the registry.");
    }

    public bool TryGet(string name, out PromptTemplate? template, string? version = null)
    {
        version ??= _latestVersion.TryGetValue(name, out var latest) ? latest : null;
        if (version is null)
        {
            template = null;
            return false;
        }

        return _byKey.TryGetValue(BuildKey(name, version), out template);
    }

    public string Render(string name, IReadOnlyDictionary<string, string> values, string? version = null) =>
        Get(name, version).Render(values);

    private static string BuildKey(string name, string version) => $"{name}::{version}";

    private static int CompareVersions(string a, string b)
    {
        // Compare numeric suffixes when both look like "vN"; otherwise fall back to ordinal.
        var na = ExtractNumber(a);
        var nb = ExtractNumber(b);
        if (na.HasValue && nb.HasValue)
        {
            return na.Value.CompareTo(nb.Value);
        }

        return string.CompareOrdinal(a, b);
    }

    private static int? ExtractNumber(string version)
    {
        var digits = new string(version.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var n) ? n : null;
    }
}
