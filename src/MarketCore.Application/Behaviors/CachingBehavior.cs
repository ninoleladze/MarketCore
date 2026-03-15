using MediatR;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;
using Microsoft.Extensions.Logging;

namespace MarketCore.Application.Behaviors;

public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICacheService _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(ICacheService cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery cacheableQuery)
            return await next();

        var cacheKey = cacheableQuery.CacheKey;

        var cached = await _cache.GetAsync<TResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        _logger.LogDebug("Cache miss for {CacheKey} — invoking handler", cacheKey);
        var response = await next();

        if (response is not IResultBase { IsFailure: true })
        {
            await _cache.SetAsync(cacheKey, response, cacheableQuery.CacheDuration, cancellationToken);
        }

        return response;
    }
}
