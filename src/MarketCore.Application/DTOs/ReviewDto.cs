namespace MarketCore.Application.DTOs;

public sealed record ReviewDto(
    Guid Id,
    Guid UserId,
    string ReviewerName,
    int Rating,
    string Comment,
    DateTime CreatedAt);
