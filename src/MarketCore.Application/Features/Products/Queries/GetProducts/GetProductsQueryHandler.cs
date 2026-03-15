using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler
    : IRequestHandler<GetProductsQuery, Result<PagedResultDto<ProductSummaryDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetProductsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<PagedResultDto<ProductSummaryDto>>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _uow.Products.SearchAsync(
            request.SearchTerm,
            request.CategoryId,
            request.Page,
            request.PageSize,
            cancellationToken);

        var summaries = items.Select(p => new ProductSummaryDto(
            Id: p.Id,
            Name: p.Name,
            Price: p.Price.Amount,
            Currency: p.Price.Currency,
            StockQuantity: p.StockQuantity,
            CategoryId: p.CategoryId,
            CategoryName: p.Category?.Name ?? string.Empty,
            ImageUrl: p.ImageUrl,
            ReviewCount: p.Reviews.Count,
            AverageRating: p.Reviews.Count > 0
                ? p.Reviews.Average(r => r.Rating)
                : 0.0));

        var paged = PagedResultDto<ProductSummaryDto>.Create(
            summaries, totalCount, request.Page, request.PageSize);

        return Result<PagedResultDto<ProductSummaryDto>>.Success(paged);
    }
}
