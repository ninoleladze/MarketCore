using MediatR;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public UpdateCategoryCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Result.Failure("Only administrators can update categories.");

        var category = await _uow.Categories.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
            return Result.Failure($"Category '{request.Id}' not found.");

        var nameConflict = await _uow.Categories.GetByNameAsync(request.Name, cancellationToken);
        if (nameConflict is not null && nameConflict.Id != request.Id)
            return Result.Failure($"A category named '{request.Name}' already exists.");

        category.UpdateDetails(request.Name, request.Description);
        _uow.Categories.Update(category);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
