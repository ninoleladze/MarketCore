using MediatR;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Orders.Commands.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId) : IRequest<Result>;
