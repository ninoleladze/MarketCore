using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace MarketCore.Api.HealthChecks;

public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer? _redis;

    public RedisHealthCheck(IConnectionMultiplexer? redis = null)
    {
        _redis = redis;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_redis is null)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "Redis is not configured. Application is running with in-memory cache fallback."));
        }

        if (_redis.IsConnected)
            return Task.FromResult(HealthCheckResult.Healthy("Redis is connected."));

        return Task.FromResult(
            HealthCheckResult.Unhealthy("Redis is configured but not currently connected."));
    }
}
