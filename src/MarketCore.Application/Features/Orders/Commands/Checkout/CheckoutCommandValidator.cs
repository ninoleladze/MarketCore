using FluentValidation;

namespace MarketCore.Application.Features.Orders.Commands.Checkout;

public sealed class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator()
    {
        RuleFor(x => x.ShippingStreet)
            .NotEmpty().WithMessage("Shipping street is required.")
            .MaximumLength(200).WithMessage("Shipping street cannot exceed 200 characters.");

        RuleFor(x => x.ShippingCity)
            .NotEmpty().WithMessage("Shipping city is required.")
            .MaximumLength(100).WithMessage("Shipping city cannot exceed 100 characters.");

        RuleFor(x => x.ShippingState)
            .NotEmpty().WithMessage("Shipping state is required.")
            .MaximumLength(100).WithMessage("Shipping state cannot exceed 100 characters.");

        RuleFor(x => x.ShippingZipCode)
            .NotEmpty().WithMessage("Shipping zip code is required.")
            .MaximumLength(20).WithMessage("Shipping zip code cannot exceed 20 characters.");

        RuleFor(x => x.ShippingCountry)
            .NotEmpty().WithMessage("Shipping country is required.")
            .MaximumLength(100).WithMessage("Shipping country cannot exceed 100 characters.");
    }
}
