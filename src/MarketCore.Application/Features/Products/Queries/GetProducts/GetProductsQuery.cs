using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Products.Queries.GetProducts;

public sealed record GetProductsQuery(
    string? SearchTerm,
    Guid? CategoryId,
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<PagedResultDto<ProductSummaryDto>>>, ICacheableQuery
{
    public string CacheKey =>
        $"products:search={SearchTerm ?? ""}:cat={CategoryId}:page={Page}:size={PageSize}";

    public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
}
