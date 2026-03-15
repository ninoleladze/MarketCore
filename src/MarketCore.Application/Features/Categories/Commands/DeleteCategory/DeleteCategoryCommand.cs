using MediatR;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Categories.Commands.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest<Result>;
