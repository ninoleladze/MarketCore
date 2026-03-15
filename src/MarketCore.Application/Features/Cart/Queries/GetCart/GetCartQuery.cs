using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Cart.Queries.GetCart;

public sealed record GetCartQuery : IRequest<Result<CartDto>>;
