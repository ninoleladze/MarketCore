using MediatR;
using MarketCore.Domain.Common;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MarketCore.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogDebug("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next();
            stopwatch.Stop();

            if (response is IResultBase { IsFailure: true } failure)
            {
                _logger.LogWarning(
                    "Handled {RequestName} with failure in {ElapsedMs}ms — {Error}",
                    requestName, stopwatch.ElapsedMilliseconds, failure.Error);
            }
            else
            {
                _logger.LogInformation(
                    "Handled {RequestName} successfully in {ElapsedMs}ms",
                    requestName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Unhandled exception in {RequestName} after {ElapsedMs}ms",
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
