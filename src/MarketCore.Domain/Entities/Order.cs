using MarketCore.Domain.Common;
using MarketCore.Domain.Enums;
using MarketCore.Domain.Events;
using MarketCore.Domain.ValueObjects;

namespace MarketCore.Domain.Entities;

public sealed class Order : AggregateRoot
{

    public Guid UserId { get; private set; }

    public Address ShippingAddress { get; private set; } = null!;

    public OrderStatus Status { get; private set; }

    public Money TotalAmount { get; private set; } = null!;

    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    private readonly List<OrderItem> _orderItems = new();

    private Order() { }

    private Order(Guid userId, Address shippingAddress) : base()
    {
        UserId = userId;
        ShippingAddress = shippingAddress;
        Status = OrderStatus.Pending;
        TotalAmount = Money.Zero("USD");
    }

    public static Order Create(Guid userId, Address shippingAddress)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        ArgumentNullException.ThrowIfNull(shippingAddress);

        return new Order(userId, shippingAddress);
    }

    public Result AddItem(Guid productId, string productName, int qty, Money unitPrice)
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure($"Items can only be added to a Pending order. Current status: {Status}.");

        if (productId == Guid.Empty)
            return Result.Failure("ProductId cannot be empty.");

        if (string.IsNullOrWhiteSpace(productName))
            return Result.Failure("Product name cannot be empty.");

        if (qty <= 0)
            return Result.Failure("Quantity must be greater than zero.");

        _orderItems.Add(new OrderItem(Id, productId, productName.Trim(), qty, unitPrice));
        RecalculateTotal();
        return Result.Success();
    }

    public Result Confirm()
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure($"Only a Pending order can be confirmed. Current status: {Status}.");

        if (_orderItems.Count == 0)
            return Result.Failure("Cannot confirm an order with no items.");

        Status = OrderStatus.Confirmed;
        RaiseDomainEvent(new OrderPlacedEvent(Id, UserId, TotalAmount));
        return Result.Success();
    }

    public Result Ship()
    {
        if (Status != OrderStatus.Confirmed)
            return Result.Failure($"Only a Confirmed order can be shipped. Current status: {Status}.");

        Status = OrderStatus.Shipped;
        return Result.Success();
    }

    public Result Deliver()
    {
        if (Status != OrderStatus.Shipped)
            return Result.Failure($"Only a Shipped order can be marked as delivered. Current status: {Status}.");

        Status = OrderStatus.Delivered;
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled)
            return Result.Failure($"An order in '{Status}' status cannot be cancelled.");

        Status = OrderStatus.Cancelled;
        return Result.Success();
    }

    private void RecalculateTotal()
    {
        if (_orderItems.Count == 0)
        {
            TotalAmount = Money.Zero("USD");
            return;
        }

        TotalAmount = _orderItems
            .Select(i => i.LineTotal())
            .Aggregate((acc, next) => acc + next);
    }
}
