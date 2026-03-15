using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Cart.Commands.AddToCart;

public sealed record AddToCartCommand(
    Guid ProductId,
    int Quantity) : IRequest<Result<CartDto>>;
