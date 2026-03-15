using MarketCore.Domain.Common;

namespace MarketCore.Domain.ValueObjects;

public sealed record Money
{

    public decimal Amount { get; }

    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Money amount cannot be negative.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency code cannot be empty.", nameof(currency));

        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
    }

    public static Money Zero(string currency) => new(0m, currency);

    public static Money operator +(Money left, Money right)
    {
        GuardSameCurrency(left, right, "+");
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        GuardSameCurrency(left, right, "-");
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public Money Multiply(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Quantity multiplier cannot be negative.", nameof(quantity));

        return new Money(Amount * quantity, Currency);
    }

    private static void GuardSameCurrency(Money left, Money right, string operation)
    {
        if (!string.Equals(left.Currency, right.Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Cannot perform '{operation}' on Money values with different currencies: " +
                $"'{left.Currency}' and '{right.Currency}'.");
        }
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}
