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

        var images = BuildImages(product.Id, product.ImageUrl);

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

    private static IReadOnlyList<string> BuildImages(Guid productId, string? imageUrl)
    {
        var seed = productId.ToString().Replace("-", "")[..8];
        var images = Enumerable.Range(1, 5)
            .Select(i => $"https://picsum.photos/seed/{seed}{i}/800/800")
            .ToList<string>();
        if (imageUrl is not null)
            images[0] = imageUrl;
        return images.AsReadOnly();
    }
}
