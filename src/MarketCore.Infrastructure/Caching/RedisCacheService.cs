using MarketCore.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Net.Sockets;
using System.Text.Json;

namespace MarketCore.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);

            if (!value.HasValue)
                return default;

            return JsonSerializer.Deserialize<T>(value.ToString(), SerializerOptions);
        }
        catch (Exception ex) when (ex is RedisException or SocketException)
        {
            _logger.LogWarning(ex, "Redis unavailable during GetAsync for key '{Key}'. Treating as cache miss.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var serialised = JsonSerializer.Serialize(value, SerializerOptions);
            await db.StringSetAsync(key, serialised, duration);
        }
        catch (Exception ex) when (ex is RedisException or SocketException)
        {
            _logger.LogWarning(ex, "Redis unavailable during SetAsync for key '{Key}'. Skipping cache write.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex) when (ex is RedisException or SocketException)
        {
            _logger.LogWarning(ex, "Redis unavailable during RemoveAsync for key '{Key}'.", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        try
        {
            var server = GetFirstServer();
            if (server is null)
            {
                _logger.LogWarning("No Redis server available for RemoveByPrefixAsync with prefix '{Prefix}'.", prefix);
                return;
            }

            var db = _redis.GetDatabase();

            await foreach (var key in server.KeysAsync(pattern: $"{prefix}*"))
            {
                await db.KeyDeleteAsync(key);
            }
        }
        catch (Exception ex) when (ex is RedisException or SocketException)
        {
            _logger.LogWarning(ex,
                "Redis unavailable during RemoveByPrefixAsync for prefix '{Prefix}'.", prefix);
        }
    }

    private IServer? GetFirstServer()
    {
        foreach (var endpoint in _redis.GetEndPoints())
        {
            var server = _redis.GetServer(endpoint);
            if (server.IsConnected)
                return server;
        }
        return null;
    }
}
