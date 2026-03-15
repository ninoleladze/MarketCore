using MarketCore.Domain.Common;

namespace MarketCore.Domain.ValueObjects;

public sealed record Address
{

    public string Street { get; }

    public string City { get; }

    public string State { get; }

    public string ZipCode { get; }

    public string Country { get; }

    public Address(string street, string city, string state, string zipCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street cannot be empty.", nameof(street));
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be empty.", nameof(city));
        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State cannot be empty.", nameof(state));
        if (string.IsNullOrWhiteSpace(zipCode))
            throw new ArgumentException("ZipCode cannot be empty.", nameof(zipCode));
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country cannot be empty.", nameof(country));

        Street = street.Trim();
        City = city.Trim();
        State = state.Trim();
        ZipCode = zipCode.Trim();
        Country = country.Trim();
    }

    public override string ToString() =>
        $"{Street}, {City}, {State} {ZipCode}, {Country}";
}
