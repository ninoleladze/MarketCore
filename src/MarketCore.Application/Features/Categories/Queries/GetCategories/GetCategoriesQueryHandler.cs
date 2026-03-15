using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Categories.Queries.GetCategories;

public sealed class GetCategoriesQueryHandler
    : IRequestHandler<GetCategoriesQuery, Result<IEnumerable<CategoryDto>>>
{
    private readonly IUnitOfWork _uow;

    public GetCategoriesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<IEnumerable<CategoryDto>>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await _uow.Categories.GetAllWithProductCountAsync(cancellationToken);

        var dtos = categories.Select(c => new CategoryDto(
            Id: c.Id,
            Name: c.Name,
            Description: c.Description,
            ProductCount: c.Products.Count));

        return Result<IEnumerable<CategoryDto>>.Success(dtos);
    }
}
