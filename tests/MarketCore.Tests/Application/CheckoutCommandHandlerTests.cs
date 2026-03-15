using FluentAssertions;
using MarketCore.Application.Features.Orders.Commands.Checkout;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Entities;
using MarketCore.Domain.Repositories;
using MarketCore.Domain.ValueObjects;
using NSubstitute;
using Xunit;
using CartEntity = MarketCore.Domain.Entities.Cart;

namespace MarketCore.Tests.Application;

/// <summary>
/// Unit tests for CheckoutCommandHandler.
/// Layer: MarketCore.Tests
///
/// All repositories and services are mocked via NSubstitute.
/// No database or infrastructure dependencies.
/// </summary>
public sealed class CheckoutCommandHandlerTests
{
    private static readonly Guid BuyerId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Money TenDollars = new(10m, "USD");

    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly ICartRepository _cartRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IProductRepository _productRepo;
    private readonly CheckoutCommandHandler _handler;

    public CheckoutCommandHandlerTests()
    {
        _uow = Substitute.For<IUnitOfWork>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _cartRepo = Substitute.For<ICartRepository>();
        _orderRepo = Substitute.For<IOrderRepository>();
        _productRepo = Substitute.For<IProductRepository>();

        _uow.Carts.Returns(_cartRepo);
        _uow.Orders.Returns(_orderRepo);
        _uow.Products.Returns(_productRepo);

        _handler = new CheckoutCommandHandler(_uow, _currentUser);
    }

    private static CheckoutCommand ValidCommand() => new(
        ShippingStreet: "100 Main St",
        ShippingCity: "Springfield",
        ShippingState: "IL",
        ShippingZipCode: "62701",
        ShippingCountry: "US");

    // ── Authentication guard ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UnauthenticatedUser_ReturnsFailure()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not authenticated");
    }

    // ── Empty cart guard ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_EmptyCart_ReturnsFailure()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(BuyerId);

        var emptyCart = CartEntity.Create(BuyerId);
        _cartRepo.GetByUserIdWithItemsAsync(BuyerId, Arg.Any<CancellationToken>())
            .Returns(emptyCart);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("empty cart");
    }

    [Fact]
    public async Task Handle_NullCart_ReturnsFailure()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(BuyerId);

        _cartRepo.GetByUserIdWithItemsAsync(BuyerId, Arg.Any<CancellationToken>())
            .Returns((CartEntity?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("empty cart");
    }

    // ── Product not found guard ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProductNoLongerExists_ReturnsFailureWithoutCommitting()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(BuyerId);

        var cart = CartEntity.Create(BuyerId);
        cart.AddItem(ProductId, 2, TenDollars);
        _cartRepo.GetByUserIdWithItemsAsync(BuyerId, Arg.Any<CancellationToken>())
            .Returns(cart);

        _productRepo.GetByIdAsync(ProductId, Arg.Any<CancellationToken>())
            .Returns((Product?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("no longer exists");

        // Transaction begun but not committed — auto-rolls back on dispose.
        await _uow.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Insufficient stock guard ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_InsufficientStock_ReturnsFailureWithoutCommitting()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(BuyerId);

        var cart = CartEntity.Create(BuyerId);
        cart.AddItem(ProductId, 10, TenDollars);  // Wants 10
        _cartRepo.GetByUserIdWithItemsAsync(BuyerId, Arg.Any<CancellationToken>())
            .Returns(cart);

        var product = Product.Create("Widget", "desc", TenDollars, 3, CategoryId);  // Only 3 in stock
        _productRepo.GetByIdAsync(ProductId, Arg.Any<CancellationToken>())
            .Returns(product);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("stock");

        // Transaction begun but not committed — auto-rolls back on dispose.
        await _uow.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCart_CreatesOrderDecrementsStockClearsCart()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(BuyerId);

        var cart = CartEntity.Create(BuyerId);
        cart.AddItem(ProductId, 2, TenDollars);
        _cartRepo.GetByUserIdWithItemsAsync(BuyerId, Arg.Any<CancellationToken>())
            .Returns(cart);

        var product = Product.Create("Widget", "A fine widget", TenDollars, 10, CategoryId);
        _productRepo.GetByIdAsync(ProductId, Arg.Any<CancellationToken>())
            .Returns(product);

        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        // Stock should have decreased by the cart quantity.
        product.StockQuantity.Should().Be(8);

        // Cart should be cleared.
        cart.Items.Should().BeEmpty();

        // Order persisted and transaction committed.
        await _orderRepo.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCart_ReturnsNewOrderGuid()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(BuyerId);

        var cart = CartEntity.Create(BuyerId);
        cart.AddItem(ProductId, 1, TenDollars);
        _cartRepo.GetByUserIdWithItemsAsync(BuyerId, Arg.Any<CancellationToken>())
            .Returns(cart);

        var product = Product.Create("Widget", "desc", TenDollars, 5, CategoryId);
        _productRepo.GetByIdAsync(ProductId, Arg.Any<CancellationToken>())
            .Returns(product);

        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }
}
