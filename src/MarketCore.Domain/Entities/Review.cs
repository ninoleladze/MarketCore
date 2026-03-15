using MarketCore.Domain.Common;

namespace MarketCore.Domain.Entities;

public sealed class Review : BaseEntity
{

    public Guid ProductId { get; private set; }

    public Guid UserId { get; private set; }

    public int Rating { get; private set; }

    public string Comment { get; private set; } = string.Empty;

    private Review() { }

    private Review(Guid productId, Guid userId, int rating, string comment) : base()
    {
        ProductId = productId;
        UserId = userId;
        Rating = rating;
        Comment = comment;
    }

    public static Review Create(Guid productId, Guid userId, int rating, string? comment = null)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty.", nameof(productId));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5 inclusive.");

        return new Review(productId, userId, rating, comment?.Trim() ?? string.Empty);
    }
}
