using FluentAssertions;
using MarketCore.Domain.Entities;
using MarketCore.Domain.Events;
using MarketCore.Domain.ValueObjects;
using Xunit;

namespace MarketCore.Tests.Domain;

/// <summary>
/// Pure domain tests for the Product aggregate.
/// Layer: MarketCore.Tests
/// </summary>
public sealed class ProductTests
{
    private static readonly Money TenUsd = new(10m, "USD");
    private static readonly Guid ValidCategoryId = Guid.NewGuid();

    // ── Product.Create ────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidArguments_ReturnsProductWithCorrectProperties()
    {
        var product = Product.Create("Widget", "A fine widget", TenUsd, 100, ValidCategoryId);

        product.Name.Should().Be("Widget");
        product.Description.Should().Be("A fine widget");
        product.Price.Should().Be(TenUsd);
        product.StockQuantity.Should().Be(100);
        product.CategoryId.Should().Be(ValidCategoryId);
        product.Reviews.Should().BeEmpty();
        product.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsArgumentException()
    {
        var act = () => Product.Create("", "desc", TenUsd, 0, ValidCategoryId);
        act.Should().Throw<ArgumentException>().WithMessage("*name*");
    }

    [Fact]
    public void Create_WithNegativeStock_ThrowsArgumentOutOfRangeException()
    {
        var act = () => Product.Create("Widget", "desc", TenUsd, -1, ValidCategoryId);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── DecreaseStock ─────────────────────────────────────────────────────────

    [Fact]
    public void DecreaseStock_ByValidAmount_ReducesStockQuantity()
    {
        var product = Product.Create("Widget", "desc", TenUsd, 10, ValidCategoryId);

        var result = product.DecreaseStock(3);

        result.IsSuccess.Should().BeTrue();
        product.StockQuantity.Should().Be(7);
    }

    [Fact]
    public void DecreaseStock_ToExactZero_RaisesStockDepletedEvent()
    {
        var product = Product.Create("Widget", "desc", TenUsd, 5, ValidCategoryId);

        product.DecreaseStock(5);

        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockDepletedEvent>();

        var evt = product.DomainEvents.OfType<StockDepletedEvent>().Single();
        evt.ProductId.Should().Be(product.Id);
        evt.ProductName.Should().Be("Widget");
    }

    [Fact]
    public void DecreaseStock_ByMoreThanAvailable_ReturnsFailure()
    {
        var product = Product.Create("Widget", "desc", TenUsd, 3, ValidCategoryId);

        var result = product.DecreaseStock(5);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Insufficient stock");
        product.StockQuantity.Should().Be(3); // unchanged
    }

    [Fact]
    public void DecreaseStock_ByZero_ReturnsFailure()
    {
        var product = Product.Create("Widget", "desc", TenUsd, 10, ValidCategoryId);

        var result = product.DecreaseStock(0);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void DecreaseStock_DoesNotRaiseEventWhenStockRemainsAboveZero()
    {
        var product = Product.Create("Widget", "desc", TenUsd, 10, ValidCategoryId);

        product.DecreaseStock(3);

        product.DomainEvents.Should().BeEmpty();
    }

    // ── IncreaseStock ─────────────────────────────────────────────────────────

    [Fact]
    public void IncreaseStock_ByValidAmount_IncreasesStockQuantity()
    {
        var product = Product.Create("Widget", "desc", TenUsd, 5, ValidCategoryId);

        var result = product.IncreaseStock(10);

        result.IsSuccess.Should().BeTrue();
        product.StockQuantity.Should().Be(15);
    }

    [Fact]
    public void IncreaseStock_ByZero_ReturnsFailure()
    {
        var product = Product.Create("Widget", "desc", TenUsd, 5, ValidCategoryId);

        var result = product.IncreaseStock(0);

        result.IsFailure.Should().BeTrue();
    }

    // ── UpdatePrice ───────────────────────────────────────────────────────────

    [Fact]
    public void UpdatePrice_WithValidMoney_UpdatesPrice()
    {
        var product = Product.Create("Widget", "desc", TenUsd, 5, ValidCategoryId);
        var newPrice = new Money(25m, "USD");

        var result = product.UpdatePrice(newPrice);

        result.IsSuccess.Should().BeTrue();
        product.Price.Should().Be(newPrice);
    }

    [Fact]
    public void UpdatePrice_WithNull_ReturnsFailure()
    {
        var product = Product.Create("Widget", "desc", TenUsd, 5, ValidCategoryId);

        var result = product.UpdatePrice(null!);

        result.IsFailure.Should().BeTrue();
    }

    // ── UpdateDetails ─────────────────────────────────────────────────────────

    [Fact]
    public void UpdateDetails_WithValidArguments_UpdatesNameAndDescription()
    {
        var product = Product.Create("Widget", "old desc", TenUsd, 5, ValidCategoryId);

        var result = product.UpdateDetails("Super Widget", "new desc");

        result.IsSuccess.Should().BeTrue();
        product.Name.Should().Be("Super Widget");
        product.Description.Should().Be("new desc");
    }

    [Fact]
    public void UpdateDetails_WithEmptyName_ReturnsFailure()
    {
        var product = Product.Create("Widget", "desc", TenUsd, 5, ValidCategoryId);

        var result = product.UpdateDetails("", "desc");

        result.IsFailure.Should().BeTrue();
    }
}
