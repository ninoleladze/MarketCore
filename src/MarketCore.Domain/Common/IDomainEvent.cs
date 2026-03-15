namespace MarketCore.Domain.Common;

public interface IDomainEvent
{

    DateTime OccurredAt { get; }
}
