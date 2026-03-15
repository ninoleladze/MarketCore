using MediatR;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;
using MarketCore.Domain.Entities;

namespace MarketCore.Application.Features.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public CreateCategoryCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Result<Guid>.Failure("Only administrators can create categories.");

        var existing = await _uow.Categories.GetByNameAsync(request.Name, cancellationToken);
        if (existing is not null)
            return Result<Guid>.Failure($"A category named '{request.Name}' already exists.");

        var category = Category.Create(request.Name, request.Description);

        await _uow.Categories.AddAsync(category, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(category.Id);
    }
}
