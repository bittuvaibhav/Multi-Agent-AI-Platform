namespace Enterprise.Agent.Persistence.Options;

/// <summary>Configuration for EF Core persistence.</summary>
public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    /// <summary>SQL Server connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Apply pending migrations automatically on startup.</summary>
    public bool AutoMigrate { get; set; } = true;
}
