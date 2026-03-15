using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Categories.Queries.GetCategories;

public sealed record GetCategoriesQuery : IRequest<Result<IEnumerable<CategoryDto>>>;
