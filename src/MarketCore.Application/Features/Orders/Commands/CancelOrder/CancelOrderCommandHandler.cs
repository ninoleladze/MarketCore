using MediatR;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Orders.Commands.CancelOrder;

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public CancelOrderCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        CancelOrderCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Failure("User is not authenticated.");

        var order = await _uow.Orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure($"Order '{request.OrderId}' not found.");

        var isOwner = order.UserId == _currentUser.UserId.Value;
        var isAdmin = _currentUser.IsInRole("Admin");

        if (!isOwner && !isAdmin)
            return Result.Failure("You are not authorised to cancel this order.");

        var cancelResult = order.Cancel();
        if (cancelResult.IsFailure)
            return cancelResult;

        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
