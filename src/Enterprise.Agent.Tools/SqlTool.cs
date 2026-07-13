using System.ComponentModel;
using System.Text;
using Enterprise.Agent.Core.Abstractions.Sql;
using Enterprise.Agent.Core.Abstractions.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Enterprise.Agent.Tools;

/// <summary>
/// Exposes safe, read-only SQL execution to agents. Every statement passes through the
/// <see cref="ISqlSafetyValidator"/> before it can reach <see cref="ISqlExecutor"/>.
/// </summary>
public sealed class SqlTool : IKernelPluginSource
{
    public const string PluginName = "Sql";

    private readonly ISqlSafetyValidator _validator;
    private readonly ISqlExecutor _executor;
    private readonly ILogger<SqlTool> _logger;

    public SqlTool(ISqlSafetyValidator validator, ISqlExecutor executor, ILogger<SqlTool> logger)
    {
        _validator = validator;
        _executor = executor;
        _logger = logger;
    }

    [KernelFunction("run_read_only_query"),
     Description("Runs a read-only SQL SELECT and returns the result as CSV. Destructive statements are rejected.")]
    public async Task<string> RunAsync(
        [Description("A single read-only SQL SELECT statement.")] string sql,
        [Description("Maximum number of rows to return.")] int maxRows = 100,
        CancellationToken cancellationToken = default)
    {
        var validation = _validator.Validate(sql);
        if (!validation.IsAllowed)
        {
            return $"SQL rejected by safety policy ({validation.Kind}): {string.Join("; ", validation.Violations)}";
        }

        try
        {
            var result = await _executor.ExecuteAsync(sql, null, maxRows, cancellationToken).ConfigureAwait(false);
            return ToCsv(result.Columns, result.Rows);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SQL tool execution failed.");
            return $"Query execution failed: {ex.Message}";
        }
    }

    public KernelPlugin BuildPlugin() => KernelPluginFactory.CreateFromObject(this, PluginName);

    private static string ToCsv(
        IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<object?>> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", columns.Select(Escape)));
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", row.Select(v => Escape(v?.ToString() ?? string.Empty))));
        }

        return builder.ToString().TrimEnd();
    }

    private static string Escape(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
}
