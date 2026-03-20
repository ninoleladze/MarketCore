using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;
using MarketCore.Domain.Entities;
using CartEntity = MarketCore.Domain.Entities.Cart;

namespace MarketCore.Application.Features.Cart.Commands.AddToCart;

public sealed class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Result<CartDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public AddToCartCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result<CartDto>> Handle(
        AddToCartCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<CartDto>.Failure("User is not authenticated.");

        var userId = _currentUser.UserId.Value;

        var product = await _uow.Products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            return Result<CartDto>.Failure($"Product '{request.ProductId}' not found.");

        if (product.StockQuantity < request.Quantity)
            return Result<CartDto>.Failure(
                $"Insufficient stock for '{product.Name}'. Requested: {request.Quantity}, Available: {product.StockQuantity}.");

        var cart = await _uow.Carts.GetByUserIdWithItemsAsync(userId, cancellationToken);
        if (cart is null)
        {
            cart = CartEntity.Create(userId);
            await _uow.Carts.AddAsync(cart, cancellationToken);
        }

        var addResult = cart.AddItem(request.ProductId, request.Quantity, product.Price);
        if (addResult.IsFailure)
            return Result<CartDto>.Failure(addResult.Error!);

        _uow.Carts.Update(cart);
        await _uow.SaveChangesAsync(cancellationToken);

        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        var productLookup = new Dictionary<Guid, Product>(productIds.Count);
        foreach (var productId in productIds)
        {
            var p = await _uow.Products.GetByIdAsync(productId, cancellationToken);
            if (p is not null)
                productLookup[productId] = p;
        }

        var total = cart.GetTotal();
        var items = cart.Items.Select(i =>
        {
            productLookup.TryGetValue(i.ProductId, out var p);
            return new CartItemDto(
                Id: i.Id,
                ProductId: i.ProductId,
                ProductName: p?.Name ?? string.Empty,
                ImageUrl: p?.ImageUrl,
                Quantity: i.Quantity,
                UnitPrice: i.UnitPrice.Amount,
                Currency: i.UnitPrice.Currency,
                LineTotal: i.LineTotal().Amount);
        }).ToList();

        var dto = new CartDto(
            Id: cart.Id,
            UserId: cart.UserId,
            Items: items,
            Total: total.Amount,
            Currency: total.Currency);

        return Result<CartDto>.Success(dto);
    }
}
