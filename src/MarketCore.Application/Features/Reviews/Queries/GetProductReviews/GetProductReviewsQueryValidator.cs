using FluentValidation;

namespace MarketCore.Application.Features.Reviews.Queries.GetProductReviews;

public sealed class GetProductReviewsQueryValidator : AbstractValidator<GetProductReviewsQuery>
{
    public GetProductReviewsQueryValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required.");
    }
}
