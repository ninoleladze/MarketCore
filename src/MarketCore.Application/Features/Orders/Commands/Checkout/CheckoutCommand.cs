using MediatR;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Orders.Commands.Checkout;

public sealed record CheckoutCommand(
    string ShippingStreet,
    string ShippingCity,
    string ShippingState,
    string ShippingZipCode,
    string ShippingCountry) : IRequest<Result<Guid>>;
