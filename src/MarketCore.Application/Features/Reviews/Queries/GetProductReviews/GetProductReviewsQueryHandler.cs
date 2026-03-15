using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Reviews.Queries.GetProductReviews;

public sealed class GetProductReviewsQueryHandler
    : IRequestHandler<GetProductReviewsQuery, Result<IEnumerable<ReviewDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetProductReviewsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<IEnumerable<ReviewDto>>> Handle(
        GetProductReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var product = await _uow.Products.GetByIdWithReviewsAsync(request.ProductId, cancellationToken);
        if (product is null)
            return Result<IEnumerable<ReviewDto>>.Failure($"Product '{request.ProductId}' not found.");

        var userIds = product.Reviews.Select(r => r.UserId).Distinct();
        var userNames = new Dictionary<Guid, string>();
        foreach (var uid in userIds)
        {
            var user = await _uow.Users.GetByIdAsync(uid, cancellationToken);
            if (user is not null)
                userNames[uid] = $"{user.FirstName} {user.LastName}";
        }

        var reviews = product.Reviews
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(
                Id: r.Id,
                UserId: r.UserId,
                ReviewerName: userNames.TryGetValue(r.UserId, out var name) ? name : "Anonymous",
                Rating: r.Rating,
                Comment: r.Comment,
                CreatedAt: r.CreatedAt));

        return Result<IEnumerable<ReviewDto>>.Success(reviews);
    }
}
