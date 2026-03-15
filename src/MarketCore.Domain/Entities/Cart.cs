using MarketCore.Domain.Common;
using MarketCore.Domain.ValueObjects;

namespace MarketCore.Domain.Entities;

public sealed class Cart : AggregateRoot
{

    public Guid UserId { get; private set; }

    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    private readonly List<CartItem> _items = new();

    private Cart() { }

    private Cart(Guid userId) : base()
    {
        UserId = userId;
    }

    public static Cart Create(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        return new Cart(userId);
    }

    public Result AddItem(Guid productId, int qty, Money unitPrice)
    {
        if (productId == Guid.Empty)
            return Result.Failure("ProductId cannot be empty.");

        if (qty <= 0)
            return Result.Failure("Quantity must be greater than zero.");

        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
        {
            existing.UpdateQuantity(existing.Quantity + qty);
        }
        else
        {
            _items.Add(new CartItem(Id, productId, qty, unitPrice));
        }

        return Result.Success();
    }

    public Result RemoveItem(Guid productId)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null)
            return Result.Failure($"No cart item found for product '{productId}'.");

        _items.Remove(item);
        return Result.Success();
    }

    public Result UpdateItemQuantity(Guid productId, int qty)
    {
        if (qty <= 0)
            return Result.Failure("Quantity must be greater than zero.");

        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null)
            return Result.Failure($"No cart item found for product '{productId}'.");

        item.UpdateQuantity(qty);
        return Result.Success();
    }

    public void Clear() => _items.Clear();

    public Money GetTotal()
    {
        if (_items.Count == 0)
            return Money.Zero("USD");

        return _items
            .Select(i => i.LineTotal())
            .Aggregate((acc, next) => acc + next);
    }
}
