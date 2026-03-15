using MediatR;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;
using MarketCore.Domain.Entities;
using MarketCore.Domain.ValueObjects;

namespace MarketCore.Application.Features.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public CreateProductCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Result<Guid>.Failure("Only administrators can create products.");

        var sellerId = _currentUser.UserId ?? Guid.Empty;

        var price = new Money(request.Price, request.Currency);
        var product = Product.Create(
            request.Name,
            request.Description,
            price,
            request.StockQuantity,
            request.CategoryId,
            sellerId,
            request.ImageUrl);

        await _uow.Products.AddAsync(product, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(product.Id);
    }
}
