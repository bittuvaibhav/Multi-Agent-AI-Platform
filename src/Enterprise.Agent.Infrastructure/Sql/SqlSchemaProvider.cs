using System.Text;
using Enterprise.Agent.Core.Abstractions.Sql;
using Enterprise.Agent.Infrastructure.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Agent.Infrastructure.Sql;

/// <summary>
/// Supplies a textual schema description to ground SQL generation. Uses a configured static
/// schema when provided; otherwise introspects INFORMATION_SCHEMA from SQL Server.
/// </summary>
public sealed class SqlSchemaProvider : ISqlSchemaProvider
{
    private readonly SqlAgentOptions _options;
    private readonly ILogger<SqlSchemaProvider> _logger;

    public SqlSchemaProvider(IOptions<SqlAgentOptions> options, ILogger<SqlSchemaProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetSchemaAsync(string? dataSource, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_options.StaticSchema))
        {
            return _options.StaticSchema;
        }

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            return "No schema is available (database is not configured).";
        }

        try
        {
            await using var conn = new SqlConnection(_options.ConnectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            const string sql =
                "SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, DATA_TYPE " +
                "FROM INFORMATION_SCHEMA.COLUMNS ORDER BY TABLE_SCHEMA, TABLE_NAME, ORDINAL_POSITION;";

            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = _options.CommandTimeoutSeconds };
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            var tables = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var table = $"{reader.GetString(0)}.{reader.GetString(1)}";
                var column = $"{reader.GetString(2)} {reader.GetString(3)}";
                if (!tables.TryGetValue(table, out var columns))
                {
                    columns = [];
                    tables[table] = columns;
                }

                columns.Add(column);
            }

            var builder = new StringBuilder();
            foreach (var (table, columns) in tables)
            {
                builder.Append("TABLE ").Append(table).Append(" (")
                    .Append(string.Join(", ", columns)).AppendLine(")");
            }

            return builder.Length == 0 ? "The database contains no user tables." : builder.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to introspect SQL schema.");
            return "Schema introspection failed; generate SQL cautiously using standard ANSI syntax.";
        }
    }
}
