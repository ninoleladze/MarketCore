using FluentValidation;
using MarketCore.Domain.Enums;

namespace MarketCore.Application.Features.Orders.Commands.UpdateOrderStatus;

public sealed class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    private static readonly string[] ValidStatuses =
        Enum.GetNames(typeof(OrderStatus));

    public UpdateOrderStatusCommandValidator()
    {
        RuleFor(c => c.OrderId)
            .NotEmpty().WithMessage("OrderId is required.");

        RuleFor(c => c.NewStatus)
            .NotEmpty().WithMessage("NewStatus is required.")
            .Must(s => ValidStatuses.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"NewStatus must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
