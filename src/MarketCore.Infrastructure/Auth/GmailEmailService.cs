using MailKit.Net.Smtp;
using MailKit.Security;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MarketCore.Infrastructure.Auth;

public sealed class GmailEmailService : IEmailService
{
    private readonly SmtpSettings _smtp;
    private readonly ILogger<GmailEmailService> _logger;

    public GmailEmailService(IOptions<SmtpSettings> smtp, ILogger<GmailEmailService> logger)
    {
        _smtp   = smtp.Value;
        _logger = logger;
    }

    public async Task SendEmailVerificationAsync(
        string toEmail,
        string firstName,
        string verificationUrl,
        CancellationToken ct = default)
    {
        var subject = "Verify your MarketCore email address";
        var html    = BuildVerificationHtml(firstName, verificationUrl);

        await SendAsync(toEmail, subject, html, ct);
    }

    public async Task SendOrderConfirmationAsync(
        string toEmail,
        Guid orderId,
        Money total,
        CancellationToken ct = default)
    {
        var subject = $"Order confirmed — #{orderId.ToString()[..8].ToUpper()}";
        var html    = BuildOrderConfirmationHtml(orderId, total);

        await SendAsync(toEmail, subject, html, ct);
    }

    private async Task SendAsync(string toEmail, string subject, string html, CancellationToken ct)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.Username));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var body = new BodyBuilder { HtmlBody = html };
            message.Body = body.ToMessageBody();

            using var client = new SmtpClient();

            await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_smtp.Username, _smtp.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(quit: true, ct);

            _logger.LogInformation(
                "[EMAIL] Sent '{Subject}' to {Recipient}",
                subject, toEmail);
        }
        catch (Exception ex)
        {

            _logger.LogError(ex,
                "[EMAIL] Failed to send '{Subject}' to {Recipient}",
                subject, toEmail);
        }
    }

    private static string BuildVerificationHtml(string firstName, string verificationUrl) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8"/>
          <meta name="viewport" content="width=device-width,initial-scale=1"/>
          <title>Verify your email — MarketCore</title>
        </head>
        <body style="margin:0;padding:0;background:#0d0d0f;font-family:'Helvetica Neue',Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#0d0d0f;padding:40px 0;">
            <tr>
              <td align="center">
                <table width="600" cellpadding="0" cellspacing="0"
                       style="background:#161618;border-radius:16px;overflow:hidden;border:1px solid #2a2a2e;">

                  <!-- Header -->
                  <tr>
                    <td style="background:linear-gradient(135deg,#b00032,#e00047);padding:40px;text-align:center;">
                      <h1 style="margin:0;font-size:28px;font-weight:700;color:#fff;letter-spacing:-0.5px;">
                        MarketCore
                      </h1>
                      <p style="margin:8px 0 0;font-size:13px;color:rgba(255,255,255,0.75);letter-spacing:0.1em;text-transform:uppercase;">
                        Confirm your email address
                      </p>
                    </td>
                  </tr>

                  <!-- Body -->
                  <tr>
                    <td style="padding:40px;">
                      <h2 style="margin:0 0 12px;font-size:22px;color:#f5f5f5;font-weight:600;">
                        Hi {firstName}, one more step!
                      </h2>
                      <p style="margin:0 0 28px;font-size:15px;line-height:1.7;color:#a0a0b0;">
                        Thanks for signing up. Click the button below to verify your email address
                        and activate your MarketCore account. This link expires in 24 hours.
                      </p>

                      <!-- Big CTA button -->
                      <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:32px;">
                        <tr>
                          <td align="center">
                            <a href="{verificationUrl}"
                               style="display:inline-block;padding:16px 44px;
                                      background:linear-gradient(135deg,#b00032,#e00047);
                                      color:#fff;font-size:16px;font-weight:700;text-decoration:none;
                                      border-radius:999px;letter-spacing:0.02em;
                                      box-shadow:0 4px 20px rgba(224,0,71,0.35);">
                              ✓ &nbsp;Verify Email Address
                            </a>
                          </td>
                        </tr>
                      </table>

                      <!-- Fallback link -->
                      <p style="margin:0 0 6px;font-size:12px;color:#555560;text-align:center;">
                        Button not working? Copy and paste this URL into your browser:
                      </p>
                      <p style="margin:0;font-size:11px;color:#e00047;text-align:center;word-break:break-all;">
                        {verificationUrl}
                      </p>
                    </td>
                  </tr>

                  <!-- Footer -->
                  <tr>
                    <td style="background:#111113;padding:20px 40px;border-top:1px solid #2a2a2e;text-align:center;">
                      <p style="margin:0 0 4px;font-size:12px;color:#555560;">
                        If you didn't create a MarketCore account, you can safely ignore this email.
                      </p>
                      <p style="margin:0;font-size:12px;color:#555560;">
                        © {DateTime.UtcNow.Year} MarketCore. All rights reserved.
                      </p>
                    </td>
                  </tr>

                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    private static string BuildOrderConfirmationHtml(Guid orderId, Money total) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="UTF-8"/><title>Order Confirmed</title></head>
        <body style="margin:0;padding:0;background:#0d0d0f;font-family:'Helvetica Neue',Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#0d0d0f;padding:40px 0;">
            <tr>
              <td align="center">
                <table width="600" cellpadding="0" cellspacing="0"
                       style="background:#161618;border-radius:16px;overflow:hidden;border:1px solid #2a2a2e;">
                  <tr>
                    <td style="background:linear-gradient(135deg,#b00032,#e00047);padding:40px;text-align:center;">
                      <h1 style="margin:0 0 4px;font-size:26px;color:#fff;font-weight:700;">Order Confirmed</h1>
                      <p style="margin:0;font-size:13px;color:rgba(255,255,255,0.7);">
                        #{orderId.ToString()[..8].ToUpper()}
                      </p>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:40px;">
                      <p style="margin:0 0 24px;font-size:15px;line-height:1.7;color:#a0a0b0;">
                        Thank you for your purchase! Your order has been received and is now being processed.
                      </p>
                      <table width="100%" cellpadding="0" cellspacing="0"
                             style="background:#1e1e22;border-radius:10px;padding:20px;margin-bottom:28px;">
                        <tr>
                          <td style="font-size:13px;color:#808090;padding:6px 0;">Order ID</td>
                          <td align="right" style="font-size:13px;color:#f0f0f5;padding:6px 0;">
                            {orderId.ToString()[..8].ToUpper()}
                          </td>
                        </tr>
                        <tr>
                          <td colspan="2" style="border-top:1px solid #2a2a2e;padding:0;"></td>
                        </tr>
                        <tr>
                          <td style="font-size:14px;font-weight:700;color:#f0f0f5;padding:10px 0 6px;">Total</td>
                          <td align="right"
                              style="font-size:16px;font-weight:700;color:#e00047;padding:10px 0 6px;">
                            {total.Amount:F2} {total.Currency}
                          </td>
                        </tr>
                      </table>
                      <table width="100%" cellpadding="0" cellspacing="0">
                        <tr>
                          <td align="center">
                            <a href="http://localhost:4200/orders/{orderId}"
                               style="display:inline-block;padding:14px 36px;
                                      background:linear-gradient(135deg,#b00032,#e00047);
                                      color:#fff;font-size:15px;font-weight:600;text-decoration:none;
                                      border-radius:999px;">
                              View Order
                            </a>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                  <tr>
                    <td style="background:#111113;padding:24px 40px;border-top:1px solid #2a2a2e;text-align:center;">
                      <p style="margin:0;font-size:12px;color:#555560;">
                        © {DateTime.UtcNow.Year} MarketCore. All rights reserved.
                      </p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
}
