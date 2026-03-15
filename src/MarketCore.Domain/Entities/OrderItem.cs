using MarketCore.Domain.Common;
using MarketCore.Domain.ValueObjects;

namespace MarketCore.Domain.Entities;

public sealed class OrderItem : BaseEntity
{

    public Guid OrderId { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public Money UnitPrice { get; private set; } = null!;

    private OrderItem() { }

    internal OrderItem(Guid orderId, Guid productId, string productName, int quantity, Money unitPrice) : base()
    {
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Money LineTotal() => UnitPrice.Multiply(Quantity);
}
