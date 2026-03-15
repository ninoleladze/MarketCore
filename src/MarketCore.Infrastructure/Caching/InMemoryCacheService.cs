using MarketCore.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MarketCore.Infrastructure.Caching;

public sealed class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemoryCacheService> _logger;

    private readonly IMemoryCache _keyIndex;

    public InMemoryCacheService(IMemoryCache cache, ILogger<InMemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;

        _keyIndex = new MemoryCache(new MemoryCacheOptions());
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(key, out T? value))
            return Task.FromResult(value);

        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken ct = default)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = duration
        };

        _cache.Set(key, value, options);
        _keyIndex.Set(key, true, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = duration
        });

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _cache.Remove(key);
        _keyIndex.Remove(key);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {

        if (_keyIndex is MemoryCache mc)
        {
            var keysToRemove = new List<string>();
            mc.GetCurrentStatistics();

            _logger.LogDebug(
                "InMemoryCacheService.RemoveByPrefixAsync('{Prefix}'): clearing all in-memory cache entries " +
                "because IMemoryCache does not support key enumeration.", prefix);

            if (_cache is MemoryCache mainCache)
                mainCache.Clear();
        }

        return Task.CompletedTask;
    }
}
