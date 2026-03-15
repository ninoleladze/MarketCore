namespace MarketCore.Application.Interfaces;

public interface ICacheableQuery
{

    string CacheKey { get; }

    TimeSpan CacheDuration { get; }
}
