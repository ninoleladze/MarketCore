using System.Text.RegularExpressions;

namespace MarketCore.Domain.ValueObjects;

public sealed record Email
{

    public string Value { get; }

    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email address cannot be empty.", nameof(value));

        var normalised = value.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(normalised))
            throw new ArgumentException($"'{value}' is not a valid email address.", nameof(value));

        Value = normalised;
    }

    public static bool TryCreate(string value, out Email? email)
    {
        try
        {
            email = new Email(value);
            return true;
        }
        catch (ArgumentException)
        {
            email = null;
            return false;
        }
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
