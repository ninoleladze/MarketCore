using FluentAssertions;
using MarketCore.Domain.Entities;
using MarketCore.Domain.Enums;
using MarketCore.Domain.Events;
using MarketCore.Domain.ValueObjects;
using Xunit;

namespace MarketCore.Tests.Domain;

/// <summary>
/// Pure domain tests for the Order aggregate.
/// No mocks, no framework dependencies — only domain types.
/// Layer: MarketCore.Tests
/// </summary>
public sealed class OrderTests
{
    private static readonly Address ValidAddress = new("123 Main St", "Springfield", "IL", "62701", "US");
    private static readonly Guid ValidUserId = Guid.NewGuid();
    private static readonly Money TenDollars = new(10m, "USD");

    // ── Order.Create ──────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidArguments_ReturnsOrderInPendingStatus()
    {
        var order = Order.Create(ValidUserId, ValidAddress);

        order.Status.Should().Be(OrderStatus.Pending);
        order.UserId.Should().Be(ValidUserId);
        order.OrderItems.Should().BeEmpty();
        order.TotalAmount.Should().Be(Money.Zero("USD"));
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentException()
    {
        var act = () => Order.Create(Guid.Empty, ValidAddress);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*UserId*");
    }

    [Fact]
    public void Create_WithNullAddress_ThrowsArgumentNullException()
    {
        var act = () => Order.Create(ValidUserId, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ── Order.AddItem ─────────────────────────────────────────────────────────

    [Fact]
    public void AddItem_ToNewPendingOrder_AddsItemAndUpdatesTotalAmount()
    {
        var order = Order.Create(ValidUserId, ValidAddress);
        var productId = Guid.NewGuid();

        var result = order.AddItem(productId, "Widget", 3, TenDollars);

        result.IsSuccess.Should().BeTrue();
        order.OrderItems.Should().HaveCount(1);
        order.TotalAmount.Amount.Should().Be(30m);
        order.TotalAmount.Currency.Should().Be("USD");
    }

    [Fact]
    public void AddItem_ToConfirmedOrder_ReturnsFailure()
    {
        var order = CreateConfirmedOrder();

        var result = order.AddItem(Guid.NewGuid(), "Late Widget", 1, TenDollars);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Pending");
    }

    [Fact]
    public void AddItem_WithZeroQuantity_ReturnsFailure()
    {
        var order = Order.Create(ValidUserId, ValidAddress);

        var result = order.AddItem(Guid.NewGuid(), "Widget", 0, TenDollars);

        result.IsFailure.Should().BeTrue();
    }

    // ── Order.Confirm ─────────────────────────────────────────────────────────

    [Fact]
    public void Confirm_PendingOrderWithItems_TransitionsToConfirmedAndRaisesEvent()
    {
        var order = Order.Create(ValidUserId, ValidAddress);
        order.AddItem(Guid.NewGuid(), "Widget", 1, TenDollars);

        var result = order.Confirm();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderPlacedEvent>();
    }

    [Fact]
    public void Confirm_EmptyPendingOrder_ReturnsFailure()
    {
        var order = Order.Create(ValidUserId, ValidAddress);

        var result = order.Confirm();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("no items");
    }

    [Fact]
    public void Confirm_AlreadyConfirmedOrder_ReturnsFailure()
    {
        var order = CreateConfirmedOrder();

        var result = order.Confirm();

        result.IsFailure.Should().BeTrue();
    }

    // ── Order.Ship ────────────────────────────────────────────────────────────

    [Fact]
    public void Ship_ConfirmedOrder_TransitionsToShipped()
    {
        var order = CreateConfirmedOrder();

        var result = order.Ship();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void Ship_PendingOrder_ReturnsFailure()
    {
        var order = Order.Create(ValidUserId, ValidAddress);

        var result = order.Ship();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Confirmed");
    }

    // ── Order.Deliver ─────────────────────────────────────────────────────────

    [Fact]
    public void Deliver_ShippedOrder_TransitionsToDelivered()
    {
        var order = CreateShippedOrder();

        var result = order.Deliver();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public void Deliver_ConfirmedOrder_ReturnsFailure()
    {
        var order = CreateConfirmedOrder();

        var result = order.Deliver();

        result.IsFailure.Should().BeTrue();
    }

    // ── Order.Cancel ──────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_PendingOrder_TransitionsToCancelled()
    {
        var order = Order.Create(ValidUserId, ValidAddress);

        var result = order.Cancel();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_ConfirmedOrder_TransitionsToCancelled()
    {
        var order = CreateConfirmedOrder();

        var result = order.Cancel();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_ShippedOrder_ReturnsFailure()
    {
        var order = CreateShippedOrder();

        var result = order.Cancel();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Shipped");
    }

    [Fact]
    public void Cancel_DeliveredOrder_ReturnsFailure()
    {
        var order = CreateShippedOrder();
        order.Deliver();

        var result = order.Cancel();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Cancel_AlreadyCancelledOrder_ReturnsFailure()
    {
        var order = Order.Create(ValidUserId, ValidAddress);
        order.Cancel();

        var result = order.Cancel();

        result.IsFailure.Should().BeTrue();
    }

    // ── Domain Events ──────────────────────────────────────────────────────────

    [Fact]
    public void OrderPlacedEvent_ContainsCorrectData()
    {
        var order = Order.Create(ValidUserId, ValidAddress);
        order.AddItem(Guid.NewGuid(), "Widget", 2, TenDollars);
        order.Confirm();

        var evt = order.DomainEvents.OfType<OrderPlacedEvent>().Single();

        evt.OrderId.Should().Be(order.Id);
        evt.UserId.Should().Be(ValidUserId);
        evt.TotalAmount.Amount.Should().Be(20m);
        evt.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var order = Order.Create(ValidUserId, ValidAddress);
        order.AddItem(Guid.NewGuid(), "Widget", 1, TenDollars);
        order.Confirm();

        order.ClearDomainEvents();

        order.DomainEvents.Should().BeEmpty();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Order CreateConfirmedOrder()
    {
        var order = Order.Create(ValidUserId, ValidAddress);
        order.AddItem(Guid.NewGuid(), "Widget", 1, TenDollars);
        order.Confirm();
        return order;
    }

    private static Order CreateShippedOrder()
    {
        var order = CreateConfirmedOrder();
        order.Ship();
        return order;
    }
}
