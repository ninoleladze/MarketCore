using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Admin.Queries.GetAdminOrders;

public sealed class GetAdminOrdersQueryHandler
    : IRequestHandler<GetAdminOrdersQuery, Result<PagedResultDto<AdminOrderSummaryDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public GetAdminOrdersQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResultDto<AdminOrderSummaryDto>>> Handle(
        GetAdminOrdersQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Result<PagedResultDto<AdminOrderSummaryDto>>.Failure(
                "Only administrators are authorised to view all orders.");

        var page     = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var allOrders = await _uow.Orders.GetAllAsync(cancellationToken).ConfigureAwait(false);

        var sorted = allOrders
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        var totalCount = sorted.Count;

        var pageItems = sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new AdminOrderSummaryDto(
                Id:          o.Id,
                UserId:      o.UserId,
                Status:      o.Status.ToString(),
                TotalAmount: o.TotalAmount.Amount,
                Currency:    o.TotalAmount.Currency,
                ItemCount:   o.OrderItems.Count,
                CreatedAt:   o.CreatedAt))
            .ToList();

        var result = PagedResultDto<AdminOrderSummaryDto>.Create(pageItems, totalCount, page, pageSize);

        return Result<PagedResultDto<AdminOrderSummaryDto>>.Success(result);
    }
}
