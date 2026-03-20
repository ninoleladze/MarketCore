namespace MarketCore.Infrastructure.Auth;

public sealed class SmtpSettings
{
    public string Host          { get; init; } = "smtp.gmail.com";
    public int    Port          { get; init; } = 465;
    public string Username      { get; init; } = string.Empty;
    public string Password      { get; init; } = string.Empty;
    public string FromName      { get; init; } = "MarketCore";
    public string ClientBaseUrl { get; init; } = "https://market-core-86ad.vercel.app";
}
