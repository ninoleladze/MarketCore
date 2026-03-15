using MarketCore.Domain.Common;

namespace MarketCore.Domain.Events;

public sealed record StockDepletedEvent : IDomainEvent
{

    public DateTime OccurredAt { get; }

    public Guid ProductId { get; }

    public string ProductName { get; }

    public StockDepletedEvent(Guid productId, string productName)
    {
        ProductId = productId;
        ProductName = productName;
        OccurredAt = DateTime.UtcNow;
    }
}
