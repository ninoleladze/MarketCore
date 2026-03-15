using FluentValidation;

namespace MarketCore.Application.Features.Cart.Commands.RemoveFromCart;

public sealed class RemoveFromCartCommandValidator : AbstractValidator<RemoveFromCartCommand>
{
    public RemoveFromCartCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required.");
    }
}
