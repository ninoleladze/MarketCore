using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Orders.Queries.GetUserOrders;

public sealed class GetUserOrdersQueryHandler
    : IRequestHandler<GetUserOrdersQuery, Result<IEnumerable<OrderSummaryDto>>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public GetUserOrdersQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result<IEnumerable<OrderSummaryDto>>> Handle(
        GetUserOrdersQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<IEnumerable<OrderSummaryDto>>.Failure("User is not authenticated.");

        var orders = await _uow.Orders.GetByUserIdAsync(
            _currentUser.UserId.Value, cancellationToken);

        var summaries = orders.Select(o => new OrderSummaryDto(
            Id: o.Id,
            Status: o.Status.ToString(),
            TotalAmount: o.TotalAmount.Amount,
            Currency: o.TotalAmount.Currency,
            ItemCount: o.OrderItems.Count,
            CreatedAt: o.CreatedAt));

        return Result<IEnumerable<OrderSummaryDto>>.Success(summaries);
    }
}
