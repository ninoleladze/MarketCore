using MediatR;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Cart.Commands.ClearCart;

public sealed class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, Result>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public ClearCartCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        ClearCartCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result.Failure("User is not authenticated.");

        var cart = await _uow.Carts.GetByUserIdWithItemsAsync(
            _currentUser.UserId.Value, cancellationToken);

        if (cart is null)
            return Result.Failure("Cart not found.");

        cart.Clear();
        _uow.Carts.Update(cart);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
