namespace MarketCore.Application.DTOs;

public sealed record AdminStatsDto(
    int TotalProducts,
    int TotalUsers,
    int TotalOrders,
    decimal TotalRevenue);

public sealed record AdminOrderSummaryDto(
    Guid Id,
    Guid UserId,
    string Status,
    decimal TotalAmount,
    string Currency,
    int ItemCount,
    DateTime CreatedAt);
