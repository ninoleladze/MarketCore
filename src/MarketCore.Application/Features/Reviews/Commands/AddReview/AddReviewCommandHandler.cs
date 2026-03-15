using MediatR;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;
using MarketCore.Domain.Entities;

namespace MarketCore.Application.Features.Reviews.Commands.AddReview;

public sealed class AddReviewCommandHandler : IRequestHandler<AddReviewCommand, Result<Guid>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public AddReviewCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(
        AddReviewCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<Guid>.Failure("User is not authenticated.");

        var userId = _currentUser.UserId.Value;

        var product = await _uow.Products.GetByIdWithReviewsAsync(request.ProductId, cancellationToken);
        if (product is null)
            return Result<Guid>.Failure($"Product '{request.ProductId}' not found.");

        var alreadyReviewed = product.Reviews.Any(r => r.UserId == userId);
        if (alreadyReviewed)
            return Result<Guid>.Failure("You have already submitted a review for this product.");

        var review = Review.Create(product.Id, userId, request.Rating, request.Comment);

        var addResult = product.AddReview(review);
        if (addResult.IsFailure)
            return Result<Guid>.Failure(addResult.Error!);

        _uow.Products.Update(product);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(review.Id);
    }
}
