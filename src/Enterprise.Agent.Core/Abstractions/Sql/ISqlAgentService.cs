using Enterprise.Agent.Models.Sql;

namespace Enterprise.Agent.Core.Abstractions.Sql;

/// <summary>
/// Orchestrates the natural-language-to-SQL pipeline:
/// plan → generate → validate → execute → summarise.
/// </summary>
public interface ISqlAgentService
{
    Task<SqlAgentResult> RunAsync(SqlAgentRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Classifies SQL and enforces the read-only safety policy (blocks destructive statements).</summary>
public interface ISqlSafetyValidator
{
    SqlValidationResult Validate(string sql);
}

/// <summary>Supplies a textual schema description used to ground SQL generation.</summary>
public interface ISqlSchemaProvider
{
    Task<string> GetSchemaAsync(string? dataSource, CancellationToken cancellationToken = default);
}

/// <summary>Executes a validated, read-only SQL query and returns a tabular result.</summary>
public interface ISqlExecutor
{
    Task<SqlQueryResult> ExecuteAsync(
        string sql, string? dataSource, int maxRows, CancellationToken cancellationToken = default);
}
