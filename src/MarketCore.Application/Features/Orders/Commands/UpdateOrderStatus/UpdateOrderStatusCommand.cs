using MediatR;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Orders.Commands.UpdateOrderStatus;

public sealed record UpdateOrderStatusCommand(
    Guid OrderId,
    string NewStatus) : IRequest<Result>;
