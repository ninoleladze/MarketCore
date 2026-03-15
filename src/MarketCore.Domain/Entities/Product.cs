using MarketCore.Domain.Common;
using MarketCore.Domain.Events;
using MarketCore.Domain.ValueObjects;

namespace MarketCore.Domain.Entities;

public sealed class Product : AggregateRoot
{

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public Money Price { get; private set; } = null!;

    public int StockQuantity { get; private set; }

    public Guid CategoryId { get; private set; }

    public Guid? SellerId { get; private set; }

    public string? ImageUrl { get; private set; }

    public bool IsActive { get; private set; }

    public Category? Category { get; private set; }

    public IReadOnlyCollection<Review> Reviews => _reviews.AsReadOnly();

    private readonly List<Review> _reviews = new();

    private Product() { }

    private Product(string name, string description, Money price, int stockQuantity, Guid categoryId, Guid? sellerId, string? imageUrl) : base()
    {
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        CategoryId = categoryId;
        SellerId = sellerId;
        ImageUrl = imageUrl;
        IsActive = true;
    }

    public static Product Create(string name, string description, Money price, int stockQuantity, Guid categoryId, Guid? sellerId = null, string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty.", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Product name cannot exceed 200 characters.", nameof(name));

        if (stockQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Initial stock quantity cannot be negative.");

        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty.", nameof(categoryId));

        if (imageUrl is not null && imageUrl.Length > 500)
            throw new ArgumentException("Image URL cannot exceed 500 characters.", nameof(imageUrl));

        return new Product(name.Trim(), description?.Trim() ?? string.Empty, price, stockQuantity, categoryId, sellerId, imageUrl?.Trim());
    }

    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure("Product is already inactive.");

        IsActive = false;
        return Result.Success();
    }

    public Result Activate()
    {
        if (IsActive)
            return Result.Failure("Product is already active.");

        IsActive = true;
        return Result.Success();
    }

    public Result DecreaseStock(int qty)
    {
        if (qty <= 0)
            return Result.Failure("Decrease quantity must be greater than zero.");

        if (qty > StockQuantity)
            return Result.Failure($"Insufficient stock. Requested: {qty}, Available: {StockQuantity}.");

        StockQuantity -= qty;

        if (StockQuantity == 0)
            RaiseDomainEvent(new StockDepletedEvent(Id, Name));

        return Result.Success();
    }

    public Result IncreaseStock(int qty)
    {
        if (qty <= 0)
            return Result.Failure("Increase quantity must be greater than zero.");

        StockQuantity += qty;
        return Result.Success();
    }

    public Result UpdatePrice(Money price)
    {
        if (price is null)
            return Result.Failure("Price cannot be null.");

        Price = price;
        return Result.Success();
    }

    public Result UpdateDetails(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Product name cannot be empty.");

        if (name.Length > 200)
            return Result.Failure("Product name cannot exceed 200 characters.");

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        return Result.Success();
    }

    public Result SetImageUrl(string? imageUrl)
    {
        if (imageUrl is not null && imageUrl.Length > 500)
            return Result.Failure("Image URL cannot exceed 500 characters.");

        ImageUrl = imageUrl?.Trim();
        return Result.Success();
    }

    public Result AddReview(Review review)
    {
        if (review is null)
            return Result.Failure("Review cannot be null.");

        if (review.ProductId != Id)
            return Result.Failure("Review does not belong to this product.");

        _reviews.Add(review);
        return Result.Success();
    }
}
