using MarketCore.Domain.Common;
using MarketCore.Domain.ValueObjects;

namespace MarketCore.Domain.Events;

public sealed record OrderPlacedEvent : IDomainEvent
{

    public DateTime OccurredAt { get; }

    public Guid OrderId { get; }

    public Guid UserId { get; }

    public Money TotalAmount { get; }

    public DateTime PlacedAt => OccurredAt;

    public OrderPlacedEvent(Guid orderId, Guid userId, Money totalAmount)
    {
        OrderId = orderId;
        UserId = userId;
        TotalAmount = totalAmount;
        OccurredAt = DateTime.UtcNow;
    }
}
