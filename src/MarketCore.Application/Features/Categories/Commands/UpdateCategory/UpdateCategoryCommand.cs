using MediatR;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string Description) : IRequest<Result>;
