using System.Text.Json;
using Enterprise.Agent.Core.Abstractions.Memory;
using Enterprise.Agent.Memory.Options;
using Enterprise.Agent.Models.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Enterprise.Agent.Memory.Redis;

/// <summary>
/// Redis-backed conversation memory. Each conversation is a Redis list of serialised
/// <see cref="MemoryRecord"/>s with a sliding expiry. Redis failures are non-fatal.
/// </summary>
public sealed class RedisConversationMemory : IConversationMemory
{
    private readonly IConnectionMultiplexer _redis;
    private readonly MemoryOptions _options;
    private readonly ILogger<RedisConversationMemory> _logger;

    public RedisConversationMemory(
        IConnectionMultiplexer redis, IOptions<MemoryOptions> options, ILogger<RedisConversationMemory> logger)
    {
        _redis = redis;
        _options = options.Value;
        _logger = logger;
    }

    public async Task AppendAsync(string conversationId, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var key = Key(conversationId);
            await db.ListRightPushAsync(key, JsonSerializer.Serialize(record)).ConfigureAwait(false);
            await db.KeyExpireAsync(key, TimeSpan.FromMinutes(_options.ConversationTtlMinutes)).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Failed to append conversation memory for {ConversationId}.", conversationId);
        }
    }

    public async Task<IReadOnlyList<MemoryRecord>> GetRecentAsync(
        string conversationId, int limit = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var values = await db.ListRangeAsync(Key(conversationId), -limit, -1).ConfigureAwait(false);
            return values
                .Select(v => v.HasValue ? JsonSerializer.Deserialize<MemoryRecord>((string)v!) : null)
                .Where(r => r is not null)
                .Select(r => r!)
                .ToArray();
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Failed to read conversation memory for {ConversationId}.", conversationId);
            return [];
        }
    }

    public async Task ClearAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _redis.GetDatabase().KeyDeleteAsync(Key(conversationId)).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Failed to clear conversation memory for {ConversationId}.", conversationId);
        }
    }

    private RedisKey Key(string conversationId) => $"{_options.KeyPrefix}:conv:{conversationId}";
}
