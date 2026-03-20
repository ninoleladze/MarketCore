using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace MarketCore.Infrastructure.Auth;

public sealed class BrevoEmailService : IEmailService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly string _clientBaseUrl;
    private readonly ILogger<BrevoEmailService> _logger;

    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public BrevoEmailService(
        HttpClient http,
        string apiKey,
        string fromEmail,
        string fromName,
        string clientBaseUrl,
        ILogger<BrevoEmailService> logger)
    {
        _http          = http;
        _apiKey        = apiKey;
        _fromEmail     = fromEmail;
        _fromName      = fromName;
        _clientBaseUrl = clientBaseUrl;
        _logger        = logger;
    }

    public Task SendEmailVerificationAsync(
        string toEmail, string firstName, string verificationToken,
        CancellationToken ct = default)
        => SendAsync(toEmail, "Verify your MarketCore email address",
            BuildVerificationHtml(firstName, verificationToken), ct);

    public Task SendOrderConfirmationAsync(
        string toEmail, Guid orderId, Money total,
        CancellationToken ct = default)
        => SendAsync(toEmail, $"Order confirmed — #{orderId.ToString()[..8].ToUpper()}",
            BuildOrderConfirmationHtml(orderId, total), ct);

    public Task SendPasswordResetAsync(
        string toEmail, string firstName, string resetUrl,
        CancellationToken ct = default)
        => SendAsync(toEmail, "Reset your password",
            BuildPasswordResetHtml(firstName, resetUrl), ct);

    private async Task SendAsync(string toEmail, string subject, string html, CancellationToken ct)
    {
        var payload = new
        {
            sender      = new { name = _fromName, email = _fromEmail },
            to          = new[] { new { email = toEmail } },
            subject,
            htmlContent = html
        };

        var json    = JsonSerializer.Serialize(payload, Json);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", _apiKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("[EMAIL] Brevo sent '{Subject}' to {Recipient}", subject, toEmail);
        }
        else
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("[EMAIL] Brevo error {Status} for '{Subject}' to {Recipient}: {Body}",
                (int)response.StatusCode, subject, toEmail, body);
            throw new InvalidOperationException($"Brevo returned {(int)response.StatusCode}: {body}");
        }
    }

    private static string BuildVerificationHtml(string firstName, string verificationToken) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="UTF-8"/><title>Verify your email — MarketCore</title></head>
        <body style="margin:0;padding:0;background:#0d0d0f;font-family:'Helvetica Neue',Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#0d0d0f;padding:40px 0;">
            <tr><td align="center">
              <table width="600" cellpadding="0" cellspacing="0"
                     style="background:#161618;border-radius:16px;overflow:hidden;border:1px solid #2a2a2e;">
                <tr>
                  <td style="background:linear-gradient(135deg,#b00032,#e00047);padding:40px;text-align:center;">
                    <h1 style="margin:0;font-size:28px;font-weight:700;color:#fff;">MarketCore</h1>
                    <p style="margin:8px 0 0;font-size:13px;color:rgba(255,255,255,0.75);text-transform:uppercase;letter-spacing:0.1em;">
                      Confirm your email address
                    </p>
                  </td>
                </tr>
                <tr>
                  <td style="padding:40px;text-align:center;">
                    <h2 style="margin:0 0 12px;font-size:22px;color:#f5f5f5;font-weight:600;">
                      Hi {firstName}, here is your code
                    </h2>
                    <p style="margin:0 0 32px;font-size:15px;line-height:1.7;color:#a0a0b0;">
                      Enter this 6-digit code to activate your MarketCore account. Expires in 24 hours.
                    </p>
                    <div style="display:inline-block;background:#0d0d0f;border:2px solid #e00047;
                                border-radius:16px;padding:24px 48px;margin-bottom:32px;">
                      <span style="font-size:42px;font-weight:800;letter-spacing:12px;color:#e00047;
                                   font-family:'Courier New',monospace;">
                        {verificationToken}
                      </span>
                    </div>
                    <p style="margin:0;font-size:13px;color:#555560;">
                      If you didn't create a MarketCore account, ignore this email.
                    </p>
                  </td>
                </tr>
                <tr>
                  <td style="background:#111113;padding:20px 40px;border-top:1px solid #2a2a2e;text-align:center;">
                    <p style="margin:0;font-size:12px;color:#555560;">© {DateTime.UtcNow.Year} MarketCore. All rights reserved.</p>
                  </td>
                </tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;

    private static string BuildPasswordResetHtml(string firstName, string resetUrl) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="UTF-8"/><title>Reset your password — MarketCore</title></head>
        <body style="margin:0;padding:0;background:#0d0d0f;font-family:'Helvetica Neue',Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#0d0d0f;padding:40px 0;">
            <tr><td align="center">
              <table width="600" cellpadding="0" cellspacing="0"
                     style="background:#161618;border-radius:16px;overflow:hidden;border:1px solid #2a2a2e;">
                <tr>
                  <td style="background:linear-gradient(135deg,#b00032,#e00047);padding:40px;text-align:center;">
                    <h1 style="margin:0;font-size:28px;font-weight:700;color:#fff;">MarketCore</h1>
                    <p style="margin:8px 0 0;font-size:13px;color:rgba(255,255,255,0.75);text-transform:uppercase;">Password reset</p>
                  </td>
                </tr>
                <tr>
                  <td style="padding:40px;">
                    <h2 style="margin:0 0 12px;font-size:22px;color:#f5f5f5;font-weight:600;">Hi {firstName}, reset your password</h2>
                    <p style="margin:0 0 28px;font-size:15px;line-height:1.7;color:#a0a0b0;">
                      Click the button below to choose a new password. Expires in 1 hour.
                    </p>
                    <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:24px;">
                      <tr><td align="center">
                        <a href="{resetUrl}" style="display:inline-block;padding:16px 44px;
                           background:linear-gradient(135deg,#b00032,#e00047);color:#fff;
                           font-size:16px;font-weight:700;text-decoration:none;border-radius:999px;">
                          Reset Password
                        </a>
                      </td></tr>
                    </table>
                    <p style="margin:0;font-size:11px;color:#e00047;text-align:center;word-break:break-all;">{resetUrl}</p>
                  </td>
                </tr>
                <tr>
                  <td style="background:#111113;padding:20px 40px;border-top:1px solid #2a2a2e;text-align:center;">
                    <p style="margin:0;font-size:12px;color:#555560;">© {DateTime.UtcNow.Year} MarketCore. All rights reserved.</p>
                  </td>
                </tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;

    private string BuildOrderConfirmationHtml(Guid orderId, Money total) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="UTF-8"/><title>Order Confirmed — MarketCore</title></head>
        <body style="margin:0;padding:0;background:#0d0d0f;font-family:'Helvetica Neue',Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#0d0d0f;padding:40px 0;">
            <tr><td align="center">
              <table width="600" cellpadding="0" cellspacing="0"
                     style="background:#161618;border-radius:16px;overflow:hidden;border:1px solid #2a2a2e;">
                <tr>
                  <td style="background:linear-gradient(135deg,#b00032,#e00047);padding:40px;text-align:center;">
                    <h1 style="margin:0 0 4px;font-size:26px;color:#fff;font-weight:700;">Order Confirmed</h1>
                    <p style="margin:0;font-size:13px;color:rgba(255,255,255,0.7);">#{orderId.ToString()[..8].ToUpper()}</p>
                  </td>
                </tr>
                <tr>
                  <td style="padding:40px;">
                    <p style="margin:0 0 24px;font-size:15px;line-height:1.7;color:#a0a0b0;">
                      Thank you for your purchase! Your order is being processed.
                    </p>
                    <table width="100%" cellpadding="0" cellspacing="0"
                           style="background:#1e1e22;border-radius:10px;padding:20px;margin-bottom:28px;">
                      <tr>
                        <td style="font-size:13px;color:#808090;padding:6px 0;">Order ID</td>
                        <td align="right" style="font-size:13px;color:#f0f0f5;">{orderId.ToString()[..8].ToUpper()}</td>
                      </tr>
                      <tr><td colspan="2" style="border-top:1px solid #2a2a2e;padding:0;"></td></tr>
                      <tr>
                        <td style="font-size:14px;font-weight:700;color:#f0f0f5;padding:10px 0 6px;">Total</td>
                        <td align="right" style="font-size:16px;font-weight:700;color:#e00047;padding:10px 0 6px;">
                          {total.Amount:F2} {total.Currency}
                        </td>
                      </tr>
                    </table>
                    <table width="100%" cellpadding="0" cellspacing="0">
                      <tr><td align="center">
                        <a href="{_clientBaseUrl}/orders/{orderId}"
                           style="display:inline-block;padding:14px 36px;
                                  background:linear-gradient(135deg,#b00032,#e00047);
                                  color:#fff;font-size:15px;font-weight:600;text-decoration:none;border-radius:999px;">
                          View Order
                        </a>
                      </td></tr>
                    </table>
                  </td>
                </tr>
                <tr>
                  <td style="background:#111113;padding:24px 40px;border-top:1px solid #2a2a2e;text-align:center;">
                    <p style="margin:0;font-size:12px;color:#555560;">© {DateTime.UtcNow.Year} MarketCore. All rights reserved.</p>
                  </td>
                </tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
}
