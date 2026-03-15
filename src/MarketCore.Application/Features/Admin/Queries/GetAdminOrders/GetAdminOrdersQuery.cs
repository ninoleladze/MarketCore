using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Admin.Queries.GetAdminOrders;

public sealed record GetAdminOrdersQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResultDto<AdminOrderSummaryDto>>>;
