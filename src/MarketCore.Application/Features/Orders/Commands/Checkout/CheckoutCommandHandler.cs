using MediatR;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;
using MarketCore.Domain.Entities;
using MarketCore.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace MarketCore.Application.Features.Orders.Commands.Checkout;

public sealed class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, Result<Guid>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IOrderHubService _orderHubService;
    private readonly ILogger<CheckoutCommandHandler> _logger;

    public CheckoutCommandHandler(
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IOrderHubService orderHubService,
        ILogger<CheckoutCommandHandler> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _orderHubService = orderHubService;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(
        CheckoutCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<Guid>.Failure("User is not authenticated.");

        var userId = _currentUser.UserId.Value;

        var cart = await _uow.Carts.GetByUserIdWithItemsAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (cart is null || !cart.Items.Any())
            return Result<Guid>.Failure("Cannot checkout with an empty cart.");

        Address shippingAddress;
        try
        {
            shippingAddress = new Address(
                request.ShippingStreet,
                request.ShippingCity,
                request.ShippingState,
                request.ShippingZipCode,
                request.ShippingCountry);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }

        await _uow.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var order = Order.Create(userId, shippingAddress);

            foreach (var item in cart.Items)
            {
                var product = await _uow.Products.GetByIdAsync(item.ProductId, cancellationToken)
                    .ConfigureAwait(false);
                if (product is null)
                    return Result<Guid>.Failure($"Product '{item.ProductId}' no longer exists.");

                var stockResult = product.DecreaseStock(item.Quantity);
                if (stockResult.IsFailure)
                    return Result<Guid>.Failure(
                        $"Insufficient stock for '{product.Name}': {stockResult.Error}");

                var addItemResult = order.AddItem(
                    product.Id,
                    product.Name,
                    item.Quantity,
                    item.UnitPrice);

                if (addItemResult.IsFailure)
                    return Result<Guid>.Failure(addItemResult.Error!);

                _uow.Products.Update(product);
            }

            var confirmResult = order.Confirm();
            if (confirmResult.IsFailure)
                return Result<Guid>.Failure(confirmResult.Error!);

            cart.Clear();

            await _uow.Orders.AddAsync(order, cancellationToken).ConfigureAwait(false);
            _uow.Carts.Update(cart);

            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await _uow.CommitAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Checkout completed. Order {OrderId} created for user {UserId}, total {TotalAmount}.",
                order.Id, userId, order.TotalAmount);

            await _orderHubService.NotifyNewOrderAsync(
                userId.ToString(),
                order.Id.ToString(),
                order.TotalAmount.Amount,
                cancellationToken).ConfigureAwait(false);

            return Result<Guid>.Success(order.Id);
        }
        catch
        {
            await _uow.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
