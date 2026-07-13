using System.Text.RegularExpressions;
using Enterprise.Agent.Core.Abstractions.Sql;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Models.Sql;

namespace Enterprise.Agent.Infrastructure.Sql;

/// <summary>
/// Enforces the read-only SQL policy. Only a single SELECT (optionally a leading CTE) is
/// permitted; any data- or schema-modifying statement, batch separator or stacked statement
/// is rejected.
/// </summary>
public sealed partial class SqlSafetyValidator : ISqlSafetyValidator
{
    private static readonly string[] ForbiddenKeywords =
    [
        "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "TRUNCATE", "CREATE", "REPLACE",
        "MERGE", "GRANT", "REVOKE", "EXEC", "EXECUTE", "SP_", "XP_", "INTO", "BACKUP",
        "RESTORE", "SHUTDOWN", "DBCC", "WAITFOR"
    ];

    public SqlValidationResult Validate(string sql)
    {
        var violations = new List<string>();
        if (string.IsNullOrWhiteSpace(sql))
        {
            return SqlValidationResult.Denied(SqlStatementKind.Unknown, ["SQL statement is empty."]);
        }

        var stripped = StripComments(sql).Trim().TrimEnd(';').Trim();

        // Reject stacked statements (a semicolon that is not the final terminator).
        if (stripped.Contains(';'))
        {
            violations.Add("Multiple SQL statements are not allowed.");
        }

        var kind = Classify(stripped);
        if (kind != SqlStatementKind.Select)
        {
            violations.Add($"Only read-only SELECT statements are permitted (detected: {kind}).");
        }

        var upper = " " + WordBoundary().Replace(stripped.ToUpperInvariant(), " ") + " ";
        foreach (var keyword in ForbiddenKeywords)
        {
            if (upper.Contains(" " + keyword + " ") || upper.Contains(" " + keyword))
            {
                if (keyword is "SP_" or "XP_")
                {
                    if (Regex.IsMatch(stripped, @"\b(sp_|xp_)", RegexOptions.IgnoreCase))
                    {
                        violations.Add($"Use of '{keyword}*' procedures is not allowed.");
                    }
                }
                else
                {
                    violations.Add($"Forbidden keyword '{keyword}' detected.");
                }
            }
        }

        return violations.Count == 0
            ? SqlValidationResult.Allowed(kind)
            : SqlValidationResult.Denied(kind, violations.Distinct().ToArray());
    }

    private static SqlStatementKind Classify(string sql)
    {
        var firstWord = sql.Split([' ', '\n', '\t', '\r', '('], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?.ToUpperInvariant() ?? string.Empty;

        return firstWord switch
        {
            "SELECT" => SqlStatementKind.Select,
            "WITH" => sql.ToUpperInvariant().Contains("SELECT") ? SqlStatementKind.Select : SqlStatementKind.Unknown,
            "INSERT" => SqlStatementKind.Insert,
            "UPDATE" => SqlStatementKind.Update,
            "DELETE" => SqlStatementKind.Delete,
            "DROP" or "ALTER" or "CREATE" or "TRUNCATE" => SqlStatementKind.Ddl,
            "GRANT" or "REVOKE" or "EXEC" or "EXECUTE" or "DBCC" => SqlStatementKind.Administrative,
            _ => SqlStatementKind.Unknown
        };
    }

    private static string StripComments(string sql)
    {
        var noBlock = BlockComment().Replace(sql, " ");
        var noLine = LineComment().Replace(noBlock, " ");
        return noLine;
    }

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex BlockComment();

    [GeneratedRegex(@"--[^\n]*")]
    private static partial Regex LineComment();

    [GeneratedRegex(@"[^A-Za-z0-9_]")]
    private static partial Regex WordBoundary();
}
