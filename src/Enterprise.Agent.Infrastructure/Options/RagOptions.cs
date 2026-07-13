namespace Enterprise.Agent.Infrastructure.Options;

/// <summary>Configuration for the RAG pipeline.</summary>
public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public string DefaultCollection { get; set; } = "knowledge";

    /// <summary>Target chunk size in characters.</summary>
    public int ChunkSize { get; set; } = 1200;

    /// <summary>Overlap between consecutive chunks in characters.</summary>
    public int ChunkOverlap { get; set; } = 200;

    /// <summary>Number of chunks retrieved for a query.</summary>
    public int TopK { get; set; } = 5;

    /// <summary>Minimum similarity score for a retrieved chunk to be used.</summary>
    public double MinScore { get; set; } = 0.0;
}
