namespace MarketCore.Infrastructure.Auth;

public sealed record JwtSettings
{

    public string Key { get; init; } = string.Empty;

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public int ExpiryDays { get; init; } = 7;
}
