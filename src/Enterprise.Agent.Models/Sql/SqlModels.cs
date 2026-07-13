using Enterprise.Agent.Models.Enums;

namespace Enterprise.Agent.Models.Sql;

/// <summary>A natural-language request to the SQL agent.</summary>
public sealed record SqlAgentRequest
{
    public required string Question { get; init; }

    /// <summary>Optional connection name/alias resolved from configuration.</summary>
    public string? DataSource { get; init; }

    public int MaxRows { get; init; } = 100;
}

/// <summary>The result of validating generated SQL against the safety policy.</summary>
public sealed record SqlValidationResult
{
    public required bool IsAllowed { get; init; }

    public SqlStatementKind Kind { get; init; }

    public IReadOnlyList<string> Violations { get; init; } = [];

    public static SqlValidationResult Allowed(SqlStatementKind kind) =>
        new() { IsAllowed = true, Kind = kind };

    public static SqlValidationResult Denied(SqlStatementKind kind, IReadOnlyList<string> violations) =>
        new() { IsAllowed = false, Kind = kind, Violations = violations };
}

/// <summary>Tabular result of executing a query.</summary>
public sealed record SqlQueryResult
{
    public required IReadOnlyList<string> Columns { get; init; }

    public required IReadOnlyList<IReadOnlyList<object?>> Rows { get; init; }

    public int RowCount => Rows.Count;
}

/// <summary>End-to-end output of the SQL agent: generated SQL, data and summary.</summary>
public sealed record SqlAgentResult
{
    public required string Question { get; init; }

    public string GeneratedSql { get; init; } = string.Empty;

    public bool Executed { get; init; }

    public SqlQueryResult? Data { get; init; }

    public string Summary { get; init; } = string.Empty;

    public IReadOnlyList<string> ValidationViolations { get; init; } = [];
}
