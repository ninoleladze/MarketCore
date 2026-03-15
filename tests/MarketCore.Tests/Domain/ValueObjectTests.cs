using FluentAssertions;
using MarketCore.Domain.ValueObjects;
using Xunit;

namespace MarketCore.Tests.Domain;

/// <summary>
/// Pure domain tests for all three Value Objects: Money, Address, Email.
/// Layer: MarketCore.Tests
/// </summary>
public sealed class ValueObjectTests
{
    // ── Money ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Money_Construction_NormalisesUpperCaseCurrency()
    {
        var money = new Money(10m, "usd");
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_NegativeAmount_ThrowsArgumentException()
    {
        var act = () => new Money(-0.01m, "USD");
        act.Should().Throw<ArgumentException>().WithMessage("*negative*");
    }

    [Fact]
    public void Money_EmptyCurrency_ThrowsArgumentException()
    {
        var act = () => new Money(10m, "");
        act.Should().Throw<ArgumentException>().WithMessage("*Currency*");
    }

    [Fact]
    public void Money_Addition_SameCurrency_ReturnsCorrectSum()
    {
        var a = new Money(10m, "USD");
        var b = new Money(5m, "USD");
        (a + b).Should().Be(new Money(15m, "USD"));
    }

    [Fact]
    public void Money_Addition_DifferentCurrencies_ThrowsInvalidOperationException()
    {
        var usd = new Money(10m, "USD");
        var eur = new Money(10m, "EUR");
        var act = () => _ = usd + eur;
        act.Should().Throw<InvalidOperationException>().WithMessage("*currencies*");
    }

    [Fact]
    public void Money_Subtraction_ResultNegative_ThrowsArgumentException()
    {
        var a = new Money(5m, "USD");
        var b = new Money(10m, "USD");
        var act = () => _ = a - b;
        act.Should().Throw<ArgumentException>().WithMessage("*negative*");
    }

    [Fact]
    public void Money_Multiply_ByPositiveQuantity_ReturnsCorrectProduct()
    {
        var price = new Money(12.50m, "USD");
        price.Multiply(4).Should().Be(new Money(50m, "USD"));
    }

    [Fact]
    public void Money_Multiply_ByNegativeQuantity_ThrowsArgumentException()
    {
        var price = new Money(10m, "USD");
        var act = () => price.Multiply(-1);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Money_StructuralEquality_EqualAmountsAndCurrency_AreEqual()
    {
        var a = new Money(10m, "USD");
        var b = new Money(10m, "USD");
        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Money_Zero_ReturnsZeroAmountWithCorrectCurrency()
    {
        var zero = Money.Zero("EUR");
        zero.Amount.Should().Be(0m);
        zero.Currency.Should().Be("EUR");
    }

    // ── Address ────────────────────────────────────────────────────────────────

    [Fact]
    public void Address_Construction_TrimsAllFields()
    {
        var address = new Address("  123 Main  ", "  NYC  ", " NY ", " 10001 ", " US ");
        address.Street.Should().Be("123 Main");
        address.City.Should().Be("NYC");
        address.Country.Should().Be("US");
    }

    [Theory]
    [InlineData("", "City", "State", "12345", "US")]
    [InlineData("Street", "", "State", "12345", "US")]
    [InlineData("Street", "City", "", "12345", "US")]
    [InlineData("Street", "City", "State", "", "US")]
    [InlineData("Street", "City", "State", "12345", "")]
    public void Address_EmptyField_ThrowsArgumentException(
        string street, string city, string state, string zip, string country)
    {
        var act = () => new Address(street, city, state, zip, country);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Address_StructuralEquality_SameValues_AreEqual()
    {
        var a = new Address("123 Main St", "City", "ST", "12345", "US");
        var b = new Address("123 Main St", "City", "ST", "12345", "US");
        a.Should().Be(b);
    }

    // ── Email ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Email_Construction_NormalisesToLowercase()
    {
        var email = new Email("Test@Example.COM");
        email.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Email_InvalidFormat_ThrowsArgumentException()
    {
        var act = () => new Email("not-an-email");
        act.Should().Throw<ArgumentException>().WithMessage("*valid email*");
    }

    [Fact]
    public void Email_EmptyString_ThrowsArgumentException()
    {
        var act = () => new Email("");
        act.Should().Throw<ArgumentException>().WithMessage("*empty*");
    }

    [Fact]
    public void Email_TryCreate_ValidEmail_ReturnsTrueAndPopulatesOut()
    {
        var success = Email.TryCreate("user@example.com", out var email);
        success.Should().BeTrue();
        email.Should().NotBeNull();
        email!.Value.Should().Be("user@example.com");
    }

    [Fact]
    public void Email_TryCreate_InvalidEmail_ReturnsFalseAndNullOut()
    {
        var success = Email.TryCreate("bad-email", out var email);
        success.Should().BeFalse();
        email.Should().BeNull();
    }

    [Fact]
    public void Email_StructuralEquality_SameNormalisedValue_AreEqual()
    {
        var a = new Email("User@Example.com");
        var b = new Email("user@example.com");
        a.Should().Be(b);
    }

    [Fact]
    public void Email_ImplicitStringConversion_ReturnsValue()
    {
        var email = new Email("user@example.com");
        string s = email;
        s.Should().Be("user@example.com");
    }
}
