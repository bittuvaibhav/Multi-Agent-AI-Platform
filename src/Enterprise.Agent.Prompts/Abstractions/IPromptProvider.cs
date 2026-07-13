using Enterprise.Agent.Models.Prompts;

namespace Enterprise.Agent.Prompts.Abstractions;

/// <summary>
/// Provides access to versioned prompt templates loaded from the prompt library.
/// Templates are addressed by name and (optionally) a specific version; when the
/// version is omitted the latest registered version is returned.
/// </summary>
public interface IPromptProvider
{
    /// <summary>All templates currently registered (latest version of each name plus explicit versions).</summary>
    IReadOnlyCollection<PromptTemplate> All { get; }

    PromptTemplate Get(string name, string? version = null);

    bool TryGet(string name, out PromptTemplate? template, string? version = null);

    /// <summary>Convenience: fetches a template and renders it with the supplied token values.</summary>
    string Render(string name, IReadOnlyDictionary<string, string> values, string? version = null);
}
