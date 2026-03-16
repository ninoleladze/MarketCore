namespace MarketCore.Application.DTOs;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int StockQuantity,
    bool IsActive,
    Guid CategoryId,
    string CategoryName,
    string? ImageUrl,
    IEnumerable<ReviewDto> Reviews,
    DateTime CreatedAt);

public sealed record ProductSummaryDto(
    Guid Id,
    string Name,
    decimal Price,
    string Currency,
    int StockQuantity,
    bool IsActive,
    Guid CategoryId,
    string CategoryName,
    string? ImageUrl,
    int ReviewCount,
    double AverageRating);
