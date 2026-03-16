using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
{
    private readonly IUnitOfWork _uow;

    public GetProductByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<ProductDto>> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        var product = await _uow.Products.GetByIdWithReviewsAsync(request.Id, cancellationToken);
        if (product is null)
            return Result<ProductDto>.Failure($"Product '{request.Id}' not found.");

        var userIds = product.Reviews.Select(r => r.UserId).Distinct();
        var userNames = new Dictionary<Guid, string>();
        foreach (var uid in userIds)
        {
            var user = await _uow.Users.GetByIdAsync(uid, cancellationToken);
            if (user is not null)
                userNames[uid] = $"{user.FirstName} {user.LastName}";
        }

        var images = BuildImages(product.ImageUrl, product.Category?.Name ?? string.Empty);

        var dto = new ProductDto(
            Id: product.Id,
            Name: product.Name,
            Description: product.Description,
            Price: product.Price.Amount,
            Currency: product.Price.Currency,
            StockQuantity: product.StockQuantity,
            IsActive: product.IsActive,
            CategoryId: product.CategoryId,
            CategoryName: product.Category?.Name ?? string.Empty,
            ImageUrl: product.ImageUrl,
            Images: images,
            Reviews: product.Reviews.Select(r => new ReviewDto(
                Id: r.Id,
                UserId: r.UserId,
                ReviewerName: userNames.TryGetValue(r.UserId, out var name) ? name : "Anonymous",
                Rating: r.Rating,
                Comment: r.Comment,
                CreatedAt: r.CreatedAt)),
            CreatedAt: product.CreatedAt);

        return Result<ProductDto>.Success(dto);
    }

    // Curated Unsplash photos grouped by category — all same visual theme
    private static readonly Dictionary<string, string[]> _categoryGallery =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Electronics"] =
            [
                "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1491553572268-6bbf32f46ab8?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1526406915894-7bcd65f60845?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1498049794561-7780e7231661?auto=format&fit=crop&w=800&q=80",
            ],
            ["Clothing"] =
            [
                "https://images.unsplash.com/photo-1558769132-cb1aea458c5e?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1489987707025-afc232f7ea0f?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1434389677669-e08b4cac3105?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1445205170230-053b83016050?auto=format&fit=crop&w=800&q=80",
            ],
            ["Books"] =
            [
                "https://images.unsplash.com/photo-1507842217343-583bb7270b66?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1481627834876-b7833e8f5570?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1524578271613-d550eacf6090?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1519682337058-a94d519337bc?auto=format&fit=crop&w=800&q=80",
            ],
            ["Digital Electronics"] =
            [
                "https://images.unsplash.com/photo-1461749280684-dccba630e2f6?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1555066931-4365d14bab8c?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1515879218367-8466d910aaa4?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1542831371-29b0f74f9713?auto=format&fit=crop&w=800&q=80",
            ],
        };

    private static IReadOnlyList<string> BuildImages(string? imageUrl, string categoryName)
    {
        var extras = _categoryGallery.TryGetValue(categoryName, out var pool)
            ? pool
            : _categoryGallery["Electronics"];

        var images = new List<string>(5);
        if (imageUrl is not null) images.Add(imageUrl);
        foreach (var url in extras)
        {
            if (images.Count == 5) break;
            images.Add(url);
        }
        return images.AsReadOnly();
    }
}
