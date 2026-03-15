using MediatR;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Cart.Commands.ClearCart;

public sealed record ClearCartCommand : IRequest<Result>;
