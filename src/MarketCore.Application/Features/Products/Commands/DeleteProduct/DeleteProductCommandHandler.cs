using MediatR;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cache;

    public DeleteProductCommandHandler(
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        ICacheService cache)
    {
        _uow = uow;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<Result> Handle(
        DeleteProductCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Result.Failure("Only administrators can delete products.");

        var product = await _uow.Products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
            return Result.Failure($"Product '{request.Id}' not found.");

        var deactivateResult = product.Deactivate();
        if (deactivateResult.IsFailure)
            return deactivateResult;

        _uow.Products.Update(product);
        await _uow.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync($"product:{request.Id}", cancellationToken);
        await _cache.RemoveByPrefixAsync("products:", cancellationToken);

        return Result.Success();
    }
}
