using FluentAssertions;
using MarketCore.Domain.Entities;
using MarketCore.Domain.ValueObjects;
using Xunit;

namespace MarketCore.Tests.Domain;

/// <summary>
/// Pure domain tests for the Cart aggregate and CartItem child entity.
/// No mocks, no framework dependencies — only domain types.
/// Layer: MarketCore.Tests
/// </summary>
public sealed class CartTests
{
    private static readonly Guid ValidUserId = Guid.NewGuid();
    private static readonly Guid ProductA = Guid.NewGuid();
    private static readonly Guid ProductB = Guid.NewGuid();
    private static readonly Money FiveDollars = new(5m, "USD");
    private static readonly Money TenDollars = new(10m, "USD");

    // ── Cart.Create ───────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidUserId_ReturnsEmptyCart()
    {
        var cart = Cart.Create(ValidUserId);

        cart.UserId.Should().Be(ValidUserId);
        cart.Items.Should().BeEmpty();
        cart.GetTotal().Should().Be(Money.Zero("USD"));
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentException()
    {
        var act = () => Cart.Create(Guid.Empty);
        act.Should().Throw<ArgumentException>().WithMessage("*UserId*");
    }

    // ── Cart.AddItem ──────────────────────────────────────────────────────────

    [Fact]
    public void AddItem_NewProduct_CreatesCartItem()
    {
        var cart = Cart.Create(ValidUserId);

        var result = cart.AddItem(ProductA, 2, TenDollars);

        result.IsSuccess.Should().BeTrue();
        cart.Items.Should().HaveCount(1);
        cart.Items.First().ProductId.Should().Be(ProductA);
        cart.Items.First().Quantity.Should().Be(2);
    }

    [Fact]
    public void AddItem_ExistingProduct_MergesQuantity()
    {
        var cart = Cart.Create(ValidUserId);
        cart.AddItem(ProductA, 2, TenDollars);

        var result = cart.AddItem(ProductA, 3, TenDollars);

        result.IsSuccess.Should().BeTrue();
        cart.Items.Should().HaveCount(1);
        cart.Items.First().Quantity.Should().Be(5);
    }

    [Fact]
    public void AddItem_DifferentProducts_CreatesSeparateItems()
    {
        var cart = Cart.Create(ValidUserId);
        cart.AddItem(ProductA, 1, TenDollars);

        cart.AddItem(ProductB, 2, FiveDollars);

        cart.Items.Should().HaveCount(2);
    }

    [Fact]
    public void AddItem_ZeroQuantity_ReturnsFailure()
    {
        var cart = Cart.Create(ValidUserId);

        var result = cart.AddItem(ProductA, 0, TenDollars);

        result.IsFailure.Should().BeTrue();
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void AddItem_EmptyProductId_ReturnsFailure()
    {
        var cart = Cart.Create(ValidUserId);

        var result = cart.AddItem(Guid.Empty, 1, TenDollars);

        result.IsFailure.Should().BeTrue();
    }

    // ── Cart.RemoveItem ───────────────────────────────────────────────────────

    [Fact]
    public void RemoveItem_ExistingProduct_RemovesItem()
    {
        var cart = Cart.Create(ValidUserId);
        cart.AddItem(ProductA, 2, TenDollars);

        var result = cart.RemoveItem(ProductA);

        result.IsSuccess.Should().BeTrue();
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void RemoveItem_NonExistentProduct_ReturnsFailure()
    {
        var cart = Cart.Create(ValidUserId);

        var result = cart.RemoveItem(ProductA);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No cart item found");
    }

    // ── Cart.UpdateItemQuantity ───────────────────────────────────────────────

    [Fact]
    public void UpdateItemQuantity_ExistingItem_UpdatesQuantity()
    {
        var cart = Cart.Create(ValidUserId);
        cart.AddItem(ProductA, 1, TenDollars);

        var result = cart.UpdateItemQuantity(ProductA, 5);

        result.IsSuccess.Should().BeTrue();
        cart.Items.First().Quantity.Should().Be(5);
    }

    [Fact]
    public void UpdateItemQuantity_ZeroQuantity_ReturnsFailure()
    {
        var cart = Cart.Create(ValidUserId);
        cart.AddItem(ProductA, 2, TenDollars);

        var result = cart.UpdateItemQuantity(ProductA, 0);

        result.IsFailure.Should().BeTrue();
        cart.Items.First().Quantity.Should().Be(2); // Unchanged.
    }

    // ── Cart.Clear ────────────────────────────────────────────────────────────

    [Fact]
    public void Clear_CartWithItems_RemovesAllItems()
    {
        var cart = Cart.Create(ValidUserId);
        cart.AddItem(ProductA, 1, TenDollars);
        cart.AddItem(ProductB, 3, FiveDollars);

        cart.Clear();

        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public void Clear_EmptyCart_IsNoOp()
    {
        var cart = Cart.Create(ValidUserId);

        cart.Clear(); // Should not throw.

        cart.Items.Should().BeEmpty();
    }

    // ── Cart.GetTotal ─────────────────────────────────────────────────────────

    [Fact]
    public void GetTotal_EmptyCart_ReturnsZeroUsd()
    {
        var cart = Cart.Create(ValidUserId);

        var total = cart.GetTotal();

        total.Amount.Should().Be(0m);
        total.Currency.Should().Be("USD");
    }

    [Fact]
    public void GetTotal_MultipleItems_ReturnsSumOfLineTotals()
    {
        var cart = Cart.Create(ValidUserId);
        cart.AddItem(ProductA, 2, TenDollars);   // 20.00
        cart.AddItem(ProductB, 3, FiveDollars);  // 15.00

        var total = cart.GetTotal();

        total.Amount.Should().Be(35m);
        total.Currency.Should().Be("USD");
    }

    [Fact]
    public void GetTotal_AfterMerge_ReflectsUpdatedQuantity()
    {
        var cart = Cart.Create(ValidUserId);
        cart.AddItem(ProductA, 1, TenDollars);  // 10.00
        cart.AddItem(ProductA, 4, TenDollars);  // merged → 5 × 10 = 50.00

        var total = cart.GetTotal();

        total.Amount.Should().Be(50m);
    }

    // ── CartItem.LineTotal ────────────────────────────────────────────────────

    [Fact]
    public void CartItem_LineTotal_ReturnsUnitPriceTimesQuantity()
    {
        var cart = Cart.Create(ValidUserId);
        cart.AddItem(ProductA, 3, TenDollars);

        var item = cart.Items.First();

        item.LineTotal().Amount.Should().Be(30m);
        item.LineTotal().Currency.Should().Be("USD");
    }
}
