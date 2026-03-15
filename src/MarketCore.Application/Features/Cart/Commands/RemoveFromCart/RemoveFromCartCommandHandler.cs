using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Cart.Commands.RemoveFromCart;

public sealed class RemoveFromCartCommandHandler : IRequestHandler<RemoveFromCartCommand, Result<CartDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public RemoveFromCartCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result<CartDto>> Handle(
        RemoveFromCartCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<CartDto>.Failure("User is not authenticated.");

        var cart = await _uow.Carts.GetByUserIdWithItemsAsync(
            _currentUser.UserId.Value, cancellationToken);

        if (cart is null)
            return Result<CartDto>.Failure("Cart not found.");

        var removeResult = cart.RemoveItem(request.ProductId);
        if (removeResult.IsFailure)
            return Result<CartDto>.Failure(removeResult.Error!);

        _uow.Carts.Update(cart);
        await _uow.SaveChangesAsync(cancellationToken);

        var total = cart.GetTotal();
        var items = cart.Items.Select(i => new CartItemDto(
            Id: i.Id,
            ProductId: i.ProductId,
            ProductName: string.Empty,
            ImageUrl: null,
            Quantity: i.Quantity,
            UnitPrice: i.UnitPrice.Amount,
            Currency: i.UnitPrice.Currency,
            LineTotal: i.LineTotal().Amount)).ToList();

        return Result<CartDto>.Success(new CartDto(
            Id: cart.Id,
            UserId: cart.UserId,
            Items: items,
            Total: total.Amount,
            Currency: total.Currency));
    }
}
