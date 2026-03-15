using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Orders.Queries.GetUserOrders;

public sealed record GetUserOrdersQuery : IRequest<Result<IEnumerable<OrderSummaryDto>>>;
