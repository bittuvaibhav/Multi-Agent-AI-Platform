namespace Enterprise.Agent.Infrastructure.Options;

/// <summary>Configuration for the natural-language SQL agent.</summary>
public sealed class SqlAgentOptions
{
    public const string SectionName = "SqlAgent";

    /// <summary>Connection string to the (read-only) reporting database.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    public int DefaultMaxRows { get; set; } = 100;

    /// <summary>
    /// Optional pre-supplied schema description. When empty, the schema is introspected from
    /// INFORMATION_SCHEMA at query time.
    /// </summary>
    public string StaticSchema { get; set; } = string.Empty;

    /// <summary>Command timeout in seconds for query execution and introspection.</summary>
    public int CommandTimeoutSeconds { get; set; } = 30;
}
