using FluentValidation;

namespace MarketCore.Application.Features.Cart.Commands.AddToCart;

public sealed class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.")
            .LessThanOrEqualTo(1000).WithMessage("Cannot add more than 1000 units in a single operation.");
    }
}
