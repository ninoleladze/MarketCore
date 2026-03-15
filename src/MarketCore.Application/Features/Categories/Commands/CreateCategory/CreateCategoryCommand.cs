using MediatR;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string Description) : IRequest<Result<Guid>>;
