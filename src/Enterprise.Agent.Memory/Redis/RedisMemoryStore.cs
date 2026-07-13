using System.Text.Json;
using Enterprise.Agent.Core.Abstractions.Memory;
using Enterprise.Agent.Memory.Options;
using Enterprise.Agent.Models.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Enterprise.Agent.Memory.Redis;

/// <summary>
/// Redis-backed long-term key/value memory store. Records are persisted under a per-owner
/// namespace and indexed in a set so they can be recalled and filtered by scope.
/// </summary>
public sealed class RedisMemoryStore : IMemoryStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly MemoryOptions _options;
    private readonly ILogger<RedisMemoryStore> _logger;

    public RedisMemoryStore(
        IConnectionMultiplexer redis, IOptions<MemoryOptions> options, ILogger<RedisMemoryStore> logger)
    {
        _redis = redis;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SaveAsync(MemoryRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var owner = Owner(record.UserId, record.ConversationId);
            var recordKey = $"{_options.KeyPrefix}:ltm:{owner}:{record.Key}";
            await db.StringSetAsync(recordKey, JsonSerializer.Serialize(record)).ConfigureAwait(false);
            await db.SetAddAsync(IndexKey(owner), recordKey).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Failed to save long-term memory '{Key}'.", record.Key);
        }
    }

    public async Task<IReadOnlyList<MemoryRecord>> QueryAsync(MemoryQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var owner = Owner(query.UserId, query.ConversationId);
            var keys = await db.SetMembersAsync(IndexKey(owner)).ConfigureAwait(false);
            if (keys.Length == 0)
            {
                return [];
            }

            var values = await db.StringGetAsync(keys.Select(k => (RedisKey)k.ToString()).ToArray()).ConfigureAwait(false);
            var records = values
                .Where(v => v.HasValue)
                .Select(v => JsonSerializer.Deserialize<MemoryRecord>((string)v!))
                .Where(r => r is not null)
                .Select(r => r!);

            if (query.Scope is { } scope)
            {
                records = records.Where(r => r.Scope == scope);
            }

            return records
                .OrderByDescending(r => r.CreatedAt)
                .Take(query.Limit)
                .ToArray();
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Failed to query long-term memory.");
            return [];
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var pattern = $"{_options.KeyPrefix}:ltm:*:{key}";
            foreach (var endpoint in _redis.GetEndPoints())
            {
                var server = _redis.GetServer(endpoint);
                await foreach (var redisKey in server.KeysAsync(pattern: pattern).WithCancellation(cancellationToken))
                {
                    await db.KeyDeleteAsync(redisKey).ConfigureAwait(false);
                }
            }
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Failed to delete long-term memory '{Key}'.", key);
        }
    }

    private static string Owner(string? userId, string? conversationId) =>
        userId ?? conversationId ?? "global";

    private RedisKey IndexKey(string owner) => $"{_options.KeyPrefix}:ltmidx:{owner}";
}
