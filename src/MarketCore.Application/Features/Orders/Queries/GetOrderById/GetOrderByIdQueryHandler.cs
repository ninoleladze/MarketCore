using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Orders.Queries.GetOrderById;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public GetOrderByIdQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result<OrderDto>> Handle(
        GetOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<OrderDto>.Failure("User is not authenticated.");

        var order = await _uow.Orders.GetByIdWithItemsAsync(request.OrderId, cancellationToken);
        if (order is null)
            return Result<OrderDto>.Failure($"Order '{request.OrderId}' not found.");

        var isOwner = order.UserId == _currentUser.UserId.Value;
        var isAdmin = _currentUser.IsInRole("Admin");

        if (!isOwner && !isAdmin)
            return Result<OrderDto>.Failure("You are not authorised to view this order.");

        var items = order.OrderItems.Select(i => new OrderItemDto(
            Id: i.Id,
            ProductId: i.ProductId,
            ProductName: i.ProductName,
            Quantity: i.Quantity,
            UnitPrice: i.UnitPrice.Amount,
            Currency: i.UnitPrice.Currency,
            LineTotal: i.LineTotal().Amount));

        var dto = new OrderDto(
            Id: order.Id,
            UserId: order.UserId,
            Status: order.Status.ToString(),
            TotalAmount: order.TotalAmount.Amount,
            Currency: order.TotalAmount.Currency,
            ShippingStreet: order.ShippingAddress.Street,
            ShippingCity: order.ShippingAddress.City,
            ShippingState: order.ShippingAddress.State,
            ShippingZipCode: order.ShippingAddress.ZipCode,
            ShippingCountry: order.ShippingAddress.Country,
            Items: items,
            CreatedAt: order.CreatedAt);

        return Result<OrderDto>.Success(dto);
    }
}
