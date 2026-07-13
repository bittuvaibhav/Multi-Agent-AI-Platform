using System.Globalization;
using System.Text;
using System.Text.Json;
using Enterprise.Agent.Core.Abstractions.VectorStore;
using Enterprise.Agent.Models.Enums;
using Enterprise.Agent.Models.VectorStore;
using Enterprise.Agent.VectorStore.Options;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace Enterprise.Agent.VectorStore.Postgres;

/// <summary>
/// PostgreSQL + pgvector implementation of <see cref="IVectorStore"/>. Embeddings are stored
/// in a <c>vector</c> column and queried with the cosine-distance operator (&lt;=&gt;). Vectors
/// are bound using pgvector's textual literal form, which is portable across Npgsql versions
/// and requires no custom type mapping.
/// </summary>
public sealed class PostgresVectorStore : IVectorStore
{
    private readonly PostgresVectorOptions _options;
    private readonly ILogger<PostgresVectorStore> _logger;

    public PostgresVectorStore(PostgresVectorOptions options, ILogger<PostgresVectorStore> logger)
    {
        _options = options;
        _logger = logger;
    }

    public VectorProvider Provider => VectorProvider.Postgres;

    public async Task EnsureCollectionAsync(string collection, int dimensions, CancellationToken cancellationToken = default)
    {
        var table = TableName(collection);
        await using var conn = await OpenAsync(cancellationToken).ConfigureAwait(false);

        await Execute(conn, "CREATE EXTENSION IF NOT EXISTS vector;", cancellationToken).ConfigureAwait(false);

        var ddl =
            "CREATE TABLE IF NOT EXISTS " + table + " (" +
            "id text PRIMARY KEY, " +
            "content text NOT NULL, " +
            "metadata jsonb NOT NULL DEFAULT '{}'::jsonb, " +
            "embedding vector(" + dimensions.ToString(CultureInfo.InvariantCulture) + ") NOT NULL);";
        await Execute(conn, ddl, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpsertAsync(IReadOnlyList<VectorRecord> records, CancellationToken cancellationToken = default)
    {
        if (records.Count == 0)
        {
            return;
        }

        await using var conn = await OpenAsync(cancellationToken).ConfigureAwait(false);
        foreach (var group in records.GroupBy(r => r.Collection))
        {
            var table = TableName(group.Key);
            foreach (var record in group)
            {
                var sql =
                    "INSERT INTO " + table + " (id, content, metadata, embedding) " +
                    "VALUES (@id, @content, @metadata::jsonb, @embedding::vector) " +
                    "ON CONFLICT (id) DO UPDATE SET content = EXCLUDED.content, " +
                    "metadata = EXCLUDED.metadata, embedding = EXCLUDED.embedding;";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("id", record.Id);
                cmd.Parameters.AddWithValue("content", record.Content);
                cmd.Parameters.Add(new NpgsqlParameter("metadata", NpgsqlDbType.Text)
                {
                    Value = JsonSerializer.Serialize(record.Metadata)
                });
                cmd.Parameters.AddWithValue("embedding", ToVectorLiteral(record.Embedding));
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        VectorQuery query, CancellationToken cancellationToken = default)
    {
        var table = TableName(query.Collection);
        await using var conn = await OpenAsync(cancellationToken).ConfigureAwait(false);

        var sql =
            "SELECT id, content, metadata, 1 - (embedding <=> @q::vector) AS score " +
            "FROM " + table + " ORDER BY embedding <=> @q::vector LIMIT @k;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("q", ToVectorLiteral(query.Embedding));
        cmd.Parameters.AddWithValue("k", query.TopK);

        var results = new List<VectorSearchResult>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var score = reader.IsDBNull(3) ? 0d : reader.GetDouble(3);
            if (score < query.MinScore)
            {
                continue;
            }

            var metadataJson = reader.IsDBNull(2) ? "{}" : reader.GetString(2);
            results.Add(new VectorSearchResult
            {
                Id = reader.GetString(0),
                Content = reader.GetString(1),
                Score = score,
                Metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson) ?? new()
            });
        }

        return results;
    }

    public async Task DeleteAsync(string collection, IReadOnlyList<string> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return;
        }

        var table = TableName(collection);
        await using var conn = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand("DELETE FROM " + table + " WHERE id = ANY(@ids);", conn);
        cmd.Parameters.AddWithValue("ids", ids.ToArray());
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var conn = new NpgsqlConnection(_options.ConnectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        return conn;
    }

    private static async Task Execute(NpgsqlConnection conn, string sql, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private string TableName(string collection)
    {
        var safe = new string((collection ?? "default").Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        if (safe.Length == 0)
        {
            safe = "default";
        }

        return _options.TablePrefix + safe.ToLowerInvariant();
    }

    private static string ToVectorLiteral(IReadOnlyList<float> embedding)
    {
        var builder = new StringBuilder(embedding.Count * 8 + 2);
        builder.Append('[');
        for (var i = 0; i < embedding.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(embedding[i].ToString("R", CultureInfo.InvariantCulture));
        }

        builder.Append(']');
        return builder.ToString();
    }
}
