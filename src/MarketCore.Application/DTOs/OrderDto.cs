namespace MarketCore.Application.DTOs;

public sealed record OrderDto(
    Guid Id,
    Guid UserId,
    string Status,
    decimal TotalAmount,
    string Currency,
    string ShippingStreet,
    string ShippingCity,
    string ShippingState,
    string ShippingZipCode,
    string ShippingCountry,
    IEnumerable<OrderItemDto> Items,
    DateTime CreatedAt);

public sealed record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    string Currency,
    decimal LineTotal);

public sealed record OrderSummaryDto(
    Guid Id,
    string Status,
    decimal TotalAmount,
    string Currency,
    int ItemCount,
    DateTime CreatedAt);
