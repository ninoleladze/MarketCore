using MarketCore.Application.Interfaces;
using MarketCore.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace MarketCore.Infrastructure.Auth;

public sealed class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailVerificationAsync(
        string toEmail,
        string firstName,
        string verificationToken,
        CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[EMAIL STUB] Verification email would be sent to {Email}. Token: {Token}. " +
            "Configure Email:Smtp settings to send real emails.",
            toEmail, verificationToken);

        return Task.CompletedTask;
    }

    public Task SendOrderConfirmationAsync(
        string toEmail,
        Guid orderId,
        Money total,
        CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[EMAIL STUB] Order confirmation would be sent to {Email} for Order {OrderId}, " +
            "Total: {Total}. Configure a real IEmailService implementation to send actual emails.",
            toEmail, orderId, total);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(
        string toEmail,
        string firstName,
        string resetUrl,
        CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[EMAIL STUB] Password reset email would be sent to {Email}. URL: {Url}. " +
            "Configure Email:Smtp settings to send real emails.",
            toEmail, resetUrl);

        return Task.CompletedTask;
    }
}
