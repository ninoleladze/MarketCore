using MediatR;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency) : IRequest<Result>;
