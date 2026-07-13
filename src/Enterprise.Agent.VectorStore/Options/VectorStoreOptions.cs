using Enterprise.Agent.Models.Enums;

namespace Enterprise.Agent.VectorStore.Options;

/// <summary>Configuration selecting and configuring the active vector store backend.</summary>
public sealed class VectorStoreOptions
{
    public const string SectionName = "VectorStore";

    /// <summary>Default provider used when a caller does not specify one.</summary>
    public VectorProvider Provider { get; set; } = VectorProvider.Postgres;

    public PostgresVectorOptions Postgres { get; set; } = new();

    public AzureSearchVectorOptions AzureAiSearch { get; set; } = new();
}

public sealed class PostgresVectorOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Table name prefix; the collection name is appended.</summary>
    public string TablePrefix { get; set; } = "vec_";
}

public sealed class AzureSearchVectorOptions
{
    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Azure AI Search REST API version.</summary>
    public string ApiVersion { get; set; } = "2024-07-01";
}
