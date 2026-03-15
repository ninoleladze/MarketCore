using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;
using MarketCore.Domain.Entities;
using CartEntity = MarketCore.Domain.Entities.Cart;

namespace MarketCore.Application.Features.Cart.Queries.GetCart;

public sealed class GetCartQueryHandler : IRequestHandler<GetCartQuery, Result<CartDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public GetCartQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result<CartDto>> Handle(
        GetCartQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<CartDto>.Failure("User is not authenticated.");

        var userId = _currentUser.UserId.Value;

        var cart = await _uow.Carts.GetByUserIdWithItemsAsync(userId, cancellationToken);
        if (cart is null)
        {
            cart = CartEntity.Create(userId);
            await _uow.Carts.AddAsync(cart, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        var productLookup = new Dictionary<Guid, Domain.Entities.Product>(productIds.Count);
        foreach (var productId in productIds)
        {
            var product = await _uow.Products.GetByIdAsync(productId, cancellationToken);
            if (product is not null)
                productLookup[productId] = product;
        }

        return Result<CartDto>.Success(MapToDto(cart, productLookup));
    }

    private static CartDto MapToDto(
        CartEntity cart,
        IReadOnlyDictionary<Guid, Domain.Entities.Product> productLookup)
    {
        var items = cart.Items.Select(i =>
        {
            productLookup.TryGetValue(i.ProductId, out var product);
            return new CartItemDto(
                Id: i.Id,
                ProductId: i.ProductId,
                ProductName: product?.Name ?? string.Empty,
                ImageUrl: product?.ImageUrl,
                Quantity: i.Quantity,
                UnitPrice: i.UnitPrice.Amount,
                Currency: i.UnitPrice.Currency,
                LineTotal: i.LineTotal().Amount);
        }).ToList();

        var total = cart.GetTotal();

        return new CartDto(
            Id: cart.Id,
            UserId: cart.UserId,
            Items: items,
            Total: total.Amount,
            Currency: total.Currency);
    }
}
