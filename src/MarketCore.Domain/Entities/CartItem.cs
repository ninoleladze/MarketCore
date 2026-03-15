using MarketCore.Domain.Common;
using MarketCore.Domain.ValueObjects;

namespace MarketCore.Domain.Entities;

public sealed class CartItem : BaseEntity
{

    public Guid CartId { get; private set; }

    public Guid ProductId { get; private set; }

    public int Quantity { get; private set; }

    public Money UnitPrice { get; private set; } = null!;

    private CartItem() { }

    internal CartItem(Guid cartId, Guid productId, int quantity, Money unitPrice) : base()
    {
        CartId = cartId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    internal void UpdateQuantity(int qty)
    {
        if (qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(qty), "Cart item quantity must be greater than zero.");

        Quantity = qty;
    }

    public Money LineTotal() => UnitPrice.Multiply(Quantity);
}
