using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Cart.Commands.RemoveFromCart;

public sealed record RemoveFromCartCommand(Guid ProductId) : IRequest<Result<CartDto>>;
