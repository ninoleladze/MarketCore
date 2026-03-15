using MediatR;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : IRequest<Result>;
