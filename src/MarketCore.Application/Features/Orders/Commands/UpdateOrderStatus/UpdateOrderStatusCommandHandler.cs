using MediatR;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;
using MarketCore.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketCore.Application.Features.Orders.Commands.UpdateOrderStatus;

public sealed class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, Result>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IOrderHubService _orderHubService;
    private readonly ILogger<UpdateOrderStatusCommandHandler> _logger;

    public UpdateOrderStatusCommandHandler(
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IOrderHubService orderHubService,
        ILogger<UpdateOrderStatusCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _orderHubService = orderHubService;
        _logger = logger;
    }

    public async Task<Result> Handle(
        UpdateOrderStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Result.Failure("Only administrators can update order status.");

        if (!Enum.TryParse<OrderStatus>(request.NewStatus, ignoreCase: true, out var targetStatus))
            return Result.Failure($"'{request.NewStatus}' is not a valid order status.");

        var order = await _uow.Orders.GetByIdAsync(request.OrderId, cancellationToken)
            .ConfigureAwait(false);
        if (order is null)
            return Result.Failure($"Order '{request.OrderId}' not found.");

        var transitionResult = targetStatus switch
        {
            OrderStatus.Confirmed => order.Confirm(),
            OrderStatus.Shipped   => order.Ship(),
            OrderStatus.Delivered => order.Deliver(),
            OrderStatus.Cancelled => order.Cancel(),
            _ => Result.Failure($"Transition to '{targetStatus}' is not supported via this command.")
        };

        if (transitionResult.IsFailure)
            return transitionResult;

        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Order {OrderId} status updated to {NewStatus} by admin.",
            order.Id, targetStatus);

        await _orderHubService.NotifyOrderStatusChangedAsync(
            order.Id.ToString(),
            order.UserId.ToString(),
            targetStatus.ToString(),
            cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
