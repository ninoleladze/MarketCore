using MediatR;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Categories.Commands.DeleteCategory;

public sealed class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public DeleteCategoryCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Result.Failure("Only administrators can delete categories.");

        var category = await _uow.Categories.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
            return Result.Failure($"Category '{request.Id}' not found.");

        if (category.Products.Count > 0)
            return Result.Failure(
                $"Cannot delete category '{category.Name}' — {category.Products.Count} product(s) are assigned to it. " +
                "Reassign or delete the products first.");

        _uow.Categories.Delete(category);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
