namespace MarketCore.Application.Options;

public sealed record JwtOptions
{
    public int ExpiryDays { get; init; } = 7;
}
