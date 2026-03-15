using MediatR;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int StockQuantity,
    Guid CategoryId,
    string? ImageUrl = null) : IRequest<Result<Guid>>;
