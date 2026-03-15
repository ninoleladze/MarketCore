using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Reviews.Queries.GetProductReviews;

public sealed record GetProductReviewsQuery(Guid ProductId)
    : IRequest<Result<IEnumerable<ReviewDto>>>;
