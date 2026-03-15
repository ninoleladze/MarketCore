using MediatR;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;
using MarketCore.Domain.ValueObjects;

namespace MarketCore.Application.Features.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public UpdateProductCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Result.Failure("Only administrators can update products.");

        var product = await _uow.Products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
            return Result.Failure($"Product '{request.Id}' not found.");

        var detailsResult = product.UpdateDetails(request.Name, request.Description);
        if (detailsResult.IsFailure)
            return detailsResult;

        var priceResult = product.UpdatePrice(new Money(request.Price, request.Currency));
        if (priceResult.IsFailure)
            return priceResult;

        _uow.Products.Update(product);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
