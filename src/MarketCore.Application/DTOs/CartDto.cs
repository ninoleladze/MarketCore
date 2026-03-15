namespace MarketCore.Application.DTOs;

public sealed record CartDto(
    Guid Id,
    Guid UserId,
    IEnumerable<CartItemDto> Items,
    decimal Total,
    string Currency);

public sealed record CartItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ImageUrl,
    int Quantity,
    decimal UnitPrice,
    string Currency,
    decimal LineTotal);
