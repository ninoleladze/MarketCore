using MediatR;
using MarketCore.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MarketCore.Application.DomainEventHandlers;

public sealed class StockDepletedEventHandler
    : INotificationHandler<DomainEventNotification<StockDepletedEvent>>
{
    private readonly ILogger<StockDepletedEventHandler> _logger;

    public StockDepletedEventHandler(ILogger<StockDepletedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(
        DomainEventNotification<StockDepletedEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;

        _logger.LogWarning(
            "Stock depleted for Product {ProductId} ('{ProductName}'). Immediate restocking recommended.",
            evt.ProductId, evt.ProductName);

        return Task.CompletedTask;
    }
}
