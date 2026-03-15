using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id)
    : IRequest<Result<ProductDto>>, ICacheableQuery
{
    public string CacheKey => $"product:{Id}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(10);
}
