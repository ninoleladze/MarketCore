using MarketCore.Domain.ValueObjects;

namespace MarketCore.Application.Interfaces;

public interface IEmailService
{

    Task SendEmailVerificationAsync(string toEmail, string firstName, string verificationUrl, CancellationToken ct = default);

    Task SendOrderConfirmationAsync(string toEmail, Guid orderId, Money total, CancellationToken ct = default);

    Task SendPasswordResetAsync(string toEmail, string firstName, string resetUrl, CancellationToken ct = default);
}
