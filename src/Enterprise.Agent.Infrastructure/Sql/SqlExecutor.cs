using Enterprise.Agent.Core.Abstractions.Sql;
using Enterprise.Agent.Infrastructure.Options;
using Enterprise.Agent.Models.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Agent.Infrastructure.Sql;

/// <summary>
/// Executes validated, read-only SQL against SQL Server and returns a bounded tabular result.
/// The connection is opened read-only and only up to <c>maxRows</c> rows are materialised.
/// </summary>
public sealed class SqlExecutor : ISqlExecutor
{
    private readonly SqlAgentOptions _options;
    private readonly ILogger<SqlExecutor> _logger;

    public SqlExecutor(IOptions<SqlAgentOptions> options, ILogger<SqlExecutor> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SqlQueryResult> ExecuteAsync(
        string sql, string? dataSource, int maxRows, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("SQL agent database connection is not configured.");
        }

        var builder = new SqlConnectionStringBuilder(_options.ConnectionString)
        {
            ApplicationIntent = ApplicationIntent.ReadOnly
        };

        await using var conn = new SqlConnection(builder.ConnectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = _options.CommandTimeoutSeconds };
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var columns = new List<string>(reader.FieldCount);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            columns.Add(reader.GetName(i));
        }

        var rows = new List<IReadOnlyList<object?>>();
        var cap = maxRows <= 0 ? _options.DefaultMaxRows : maxRows;
        while (rows.Count < cap && await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var row = new object?[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.GetValue(i);
                row[i] = value == DBNull.Value ? null : value;
            }

            rows.Add(row);
        }

        _logger.LogInformation("SQL executed; returned {Rows} row(s).", rows.Count);
        return new SqlQueryResult { Columns = columns, Rows = rows };
    }
}
