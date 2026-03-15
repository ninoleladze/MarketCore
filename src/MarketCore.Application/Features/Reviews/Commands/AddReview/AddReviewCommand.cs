using MediatR;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Reviews.Commands.AddReview;

public sealed record AddReviewCommand(
    Guid ProductId,
    int Rating,
    string Comment) : IRequest<Result<Guid>>;
