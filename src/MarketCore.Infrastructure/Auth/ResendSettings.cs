namespace MarketCore.Infrastructure.Auth;

public sealed class ResendSettings
{
    public string ApiKey        { get; init; } = string.Empty;
    public string FromAddress   { get; init; } = "onboarding@resend.dev";
    public string FromName      { get; init; } = "MarketCore";
    public string ClientBaseUrl { get; init; } = "https://market-core-86ad.vercel.app";
}
