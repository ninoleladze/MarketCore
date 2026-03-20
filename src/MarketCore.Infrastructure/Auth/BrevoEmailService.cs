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
        <head>
          <meta charset="UTF-8"/>
          <meta name="viewport" content="width=device-width,initial-scale=1"/>
          <title>Verify your email — MarketCore</title>
        </head>
        <body style="margin:0;padding:0;background:#050c1a;font-family:'Helvetica Neue',Arial,sans-serif;-webkit-font-smoothing:antialiased;">
          <table width="100%" cellpadding="0" cellspacing="0" border="0" style="background:#050c1a;padding:48px 0;">
            <tr>
              <td align="center" style="padding:0 16px;">
                <table cellpadding="0" cellspacing="0" border="0" style="width:100%;max-width:600px;background:#0d1f3c;border-radius:20px;overflow:hidden;border:1px solid #1c3868;">

                  <!-- Header -->
                  <tr>
                    <td style="background:linear-gradient(135deg,#7c3b00 0%,#c45c00 55%,#e87722 100%);padding:36px 48px;text-align:center;">
                      <table width="100%" cellpadding="0" cellspacing="0" border="0">
                        <tr>
                          <td align="center" style="padding-bottom:14px;">
                            <table cellpadding="0" cellspacing="0" border="0">
                              <tr>
                                <td style="width:50px;height:50px;background:rgba(0,0,0,0.22);border-radius:12px;border:1px solid rgba(255,255,255,0.22);text-align:center;vertical-align:middle;">
                                  <span style="font-size:22px;font-weight:800;color:#fff;font-family:Georgia,serif;line-height:50px;display:block;">M</span>
                                </td>
                              </tr>
                            </table>
                          </td>
                        </tr>
                        <tr>
                          <td align="center">
                            <h1 style="margin:0 0 4px;font-size:26px;font-weight:700;color:#fff;letter-spacing:-0.4px;font-family:Georgia,'Times New Roman',serif;">MarketCore</h1>
                            <p style="margin:0;font-size:11px;color:rgba(255,255,255,0.65);text-transform:uppercase;letter-spacing:0.2em;font-weight:500;">Premium Marketplace</p>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>

                  <!-- Divider -->
                  <tr>
                    <td style="height:1px;font-size:0;line-height:0;background:linear-gradient(90deg,#071428 0%,#e87722 30%,#ff9a3c 50%,#e87722 70%,#071428 100%);">&nbsp;</td>
                  </tr>

                  <!-- Body -->
                  <tr>
                    <td style="padding:44px 48px 40px;text-align:center;">
                      <p style="margin:0 0 14px;font-size:11px;font-weight:600;color:#ff9a3c;text-transform:uppercase;letter-spacing:0.2em;">— Email Verification —</p>
                      <h2 style="margin:0 0 14px;font-size:24px;color:#f0f4ff;font-weight:700;font-family:Georgia,'Times New Roman',serif;letter-spacing:-0.3px;">Hi {firstName}, here's your code</h2>
                      <p style="margin:0 0 36px;font-size:15px;line-height:1.75;color:#7a94b8;">Enter this 6-digit code to verify your MarketCore account.<br/>Valid for <span style="color:#c4d0e8;font-weight:600;">24 hours</span>.</p>

                      <!-- Code box -->
                      <table cellpadding="0" cellspacing="0" border="0" style="margin:0 auto 36px;">
                        <tr>
                          <td style="background:#071428;border:1px solid #1c3868;border-top:2px solid #e87722;border-radius:14px;padding:28px 52px;text-align:center;">
                            <p style="margin:0 0 12px;font-size:10px;color:#3a5478;text-transform:uppercase;letter-spacing:0.2em;font-weight:600;">Verification Code</p>
                            <p style="margin:0;font-size:46px;font-weight:800;letter-spacing:14px;color:#ff9a3c;font-family:'Courier New',Courier,monospace;line-height:1;padding-right:0;">{verificationToken}</p>
                          </td>
                        </tr>
                      </table>

                      <p style="margin:0;font-size:13px;color:#3a5478;line-height:1.6;">Didn't create a MarketCore account?<br/>You can safely ignore this email.</p>
                    </td>
                  </tr>

                  <!-- Footer -->
                  <tr>
                    <td style="background:#071428;padding:22px 48px;border-top:1px solid #1c3868;text-align:center;">
                      <p style="margin:0 0 4px;font-size:12px;color:#5a7498;">© {DateTime.UtcNow.Year} MarketCore. All rights reserved.</p>
                      <p style="margin:0;font-size:11px;color:#3a5478;">This is an automated message — please do not reply.</p>
                    </td>
                  </tr>

                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    private static string BuildPasswordResetHtml(string firstName, string resetUrl) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8"/>
          <meta name="viewport" content="width=device-width,initial-scale=1"/>
          <title>Reset your password — MarketCore</title>
        </head>
        <body style="margin:0;padding:0;background:#050c1a;font-family:'Helvetica Neue',Arial,sans-serif;-webkit-font-smoothing:antialiased;">
          <table width="100%" cellpadding="0" cellspacing="0" border="0" style="background:#050c1a;padding:48px 0;">
            <tr>
              <td align="center" style="padding:0 16px;">
                <table cellpadding="0" cellspacing="0" border="0" style="width:100%;max-width:600px;background:#0d1f3c;border-radius:20px;overflow:hidden;border:1px solid #1c3868;">

                  <!-- Header -->
                  <tr>
                    <td style="background:linear-gradient(135deg,#7c3b00 0%,#c45c00 55%,#e87722 100%);padding:36px 48px;text-align:center;">
                      <table width="100%" cellpadding="0" cellspacing="0" border="0">
                        <tr>
                          <td align="center" style="padding-bottom:14px;">
                            <table cellpadding="0" cellspacing="0" border="0">
                              <tr>
                                <td style="width:50px;height:50px;background:rgba(0,0,0,0.22);border-radius:12px;border:1px solid rgba(255,255,255,0.22);text-align:center;vertical-align:middle;">
                                  <span style="font-size:22px;font-weight:800;color:#fff;font-family:Georgia,serif;line-height:50px;display:block;">M</span>
                                </td>
                              </tr>
                            </table>
                          </td>
                        </tr>
                        <tr>
                          <td align="center">
                            <h1 style="margin:0 0 4px;font-size:26px;font-weight:700;color:#fff;letter-spacing:-0.4px;font-family:Georgia,'Times New Roman',serif;">MarketCore</h1>
                            <p style="margin:0;font-size:11px;color:rgba(255,255,255,0.65);text-transform:uppercase;letter-spacing:0.2em;font-weight:500;">Premium Marketplace</p>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>

                  <!-- Divider -->
                  <tr>
                    <td style="height:1px;font-size:0;line-height:0;background:linear-gradient(90deg,#071428 0%,#e87722 30%,#ff9a3c 50%,#e87722 70%,#071428 100%);">&nbsp;</td>
                  </tr>

                  <!-- Body -->
                  <tr>
                    <td style="padding:44px 48px 40px;text-align:center;">
                      <p style="margin:0 0 14px;font-size:11px;font-weight:600;color:#ff9a3c;text-transform:uppercase;letter-spacing:0.2em;">— Password Reset —</p>
                      <h2 style="margin:0 0 14px;font-size:24px;color:#f0f4ff;font-weight:700;font-family:Georgia,'Times New Roman',serif;letter-spacing:-0.3px;">Hi {firstName}, let's reset your password</h2>
                      <p style="margin:0 0 36px;font-size:15px;line-height:1.75;color:#7a94b8;">Click the button below to choose a new password.<br/>This link expires in <span style="color:#c4d0e8;font-weight:600;">1 hour</span>.</p>

                      <!-- CTA Button -->
                      <table cellpadding="0" cellspacing="0" border="0" style="margin:0 auto 28px;">
                        <tr>
                          <td style="background:linear-gradient(135deg,#c45c00,#e87722);border-radius:999px;padding:1px;">
                            <a href="{resetUrl}" style="display:inline-block;padding:15px 44px;background:linear-gradient(135deg,#c45c00,#e87722);color:#fff;font-size:15px;font-weight:700;text-decoration:none;border-radius:999px;letter-spacing:0.02em;">Reset Password</a>
                          </td>
                        </tr>
                      </table>

                      <!-- Fallback URL -->
                      <table cellpadding="0" cellspacing="0" border="0" style="margin:0 auto 32px;width:100%;max-width:460px;">
                        <tr>
                          <td style="background:#071428;border:1px solid #1c3868;border-radius:8px;padding:12px 16px;text-align:center;">
                            <p style="margin:0 0 4px;font-size:10px;color:#3a5478;text-transform:uppercase;letter-spacing:0.15em;">Or copy this link</p>
                            <p style="margin:0;font-size:11px;color:#7a94b8;word-break:break-all;line-height:1.5;">{resetUrl}</p>
                          </td>
                        </tr>
                      </table>

                      <p style="margin:0;font-size:13px;color:#3a5478;line-height:1.6;">Didn't request a password reset?<br/>Your account is safe — you can ignore this email.</p>
                    </td>
                  </tr>

                  <!-- Footer -->
                  <tr>
                    <td style="background:#071428;padding:22px 48px;border-top:1px solid #1c3868;text-align:center;">
                      <p style="margin:0 0 4px;font-size:12px;color:#5a7498;">© {DateTime.UtcNow.Year} MarketCore. All rights reserved.</p>
                      <p style="margin:0;font-size:11px;color:#3a5478;">This is an automated message — please do not reply.</p>
                    </td>
                  </tr>

                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    private string BuildOrderConfirmationHtml(Guid orderId, Money total) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8"/>
          <meta name="viewport" content="width=device-width,initial-scale=1"/>
          <title>Order Confirmed — MarketCore</title>
        </head>
        <body style="margin:0;padding:0;background:#050c1a;font-family:'Helvetica Neue',Arial,sans-serif;-webkit-font-smoothing:antialiased;">
          <table width="100%" cellpadding="0" cellspacing="0" border="0" style="background:#050c1a;padding:48px 0;">
            <tr>
              <td align="center" style="padding:0 16px;">
                <table cellpadding="0" cellspacing="0" border="0" style="width:100%;max-width:600px;background:#0d1f3c;border-radius:20px;overflow:hidden;border:1px solid #1c3868;">

                  <!-- Header -->
                  <tr>
                    <td style="background:linear-gradient(135deg,#7c3b00 0%,#c45c00 55%,#e87722 100%);padding:36px 48px;text-align:center;">
                      <table width="100%" cellpadding="0" cellspacing="0" border="0">
                        <tr>
                          <td align="center" style="padding-bottom:14px;">
                            <table cellpadding="0" cellspacing="0" border="0">
                              <tr>
                                <td style="width:50px;height:50px;background:rgba(0,0,0,0.22);border-radius:12px;border:1px solid rgba(255,255,255,0.22);text-align:center;vertical-align:middle;">
                                  <span style="font-size:22px;font-weight:800;color:#fff;font-family:Georgia,serif;line-height:50px;display:block;">M</span>
                                </td>
                              </tr>
                            </table>
                          </td>
                        </tr>
                        <tr>
                          <td align="center">
                            <h1 style="margin:0 0 4px;font-size:26px;font-weight:700;color:#fff;letter-spacing:-0.4px;font-family:Georgia,'Times New Roman',serif;">MarketCore</h1>
                            <p style="margin:0;font-size:11px;color:rgba(255,255,255,0.65);text-transform:uppercase;letter-spacing:0.2em;font-weight:500;">Premium Marketplace</p>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>

                  <!-- Divider -->
                  <tr>
                    <td style="height:1px;font-size:0;line-height:0;background:linear-gradient(90deg,#071428 0%,#e87722 30%,#ff9a3c 50%,#e87722 70%,#071428 100%);">&nbsp;</td>
                  </tr>

                  <!-- Body -->
                  <tr>
                    <td style="padding:44px 48px 40px;text-align:center;">
                      <p style="margin:0 0 14px;font-size:11px;font-weight:600;color:#ff9a3c;text-transform:uppercase;letter-spacing:0.2em;">— Order Confirmed —</p>
                      <h2 style="margin:0 0 14px;font-size:24px;color:#f0f4ff;font-weight:700;font-family:Georgia,'Times New Roman',serif;letter-spacing:-0.3px;">Thank you for your purchase!</h2>
                      <p style="margin:0 0 32px;font-size:15px;line-height:1.75;color:#7a94b8;">Your order has been received and is now being processed.<br/>We'll keep you updated on every step.</p>

                      <!-- Order details card -->
                      <table cellpadding="0" cellspacing="0" border="0" style="width:100%;margin-bottom:32px;background:#071428;border-radius:12px;border:1px solid #1c3868;overflow:hidden;">
                        <tr>
                          <td style="padding:16px 24px;border-bottom:1px solid #1c3868;">
                            <table width="100%" cellpadding="0" cellspacing="0" border="0">
                              <tr>
                                <td style="font-size:12px;color:#5a7498;text-transform:uppercase;letter-spacing:0.1em;font-weight:600;">Order ID</td>
                                <td align="right" style="font-size:13px;color:#c4d0e8;font-weight:600;font-family:'Courier New',monospace;">#{orderId.ToString()[..8].ToUpper()}</td>
                              </tr>
                            </table>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:16px 24px;border-bottom:1px solid #1c3868;">
                            <table width="100%" cellpadding="0" cellspacing="0" border="0">
                              <tr>
                                <td style="font-size:12px;color:#5a7498;text-transform:uppercase;letter-spacing:0.1em;font-weight:600;">Status</td>
                                <td align="right">
                                  <span style="display:inline-block;padding:3px 12px;background:rgba(232,119,34,0.15);border:1px solid rgba(232,119,34,0.3);border-radius:999px;font-size:12px;font-weight:700;color:#ff9a3c;text-transform:uppercase;letter-spacing:0.08em;">Processing</span>
                                </td>
                              </tr>
                            </table>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:20px 24px;">
                            <table width="100%" cellpadding="0" cellspacing="0" border="0">
                              <tr>
                                <td style="font-size:14px;color:#c4d0e8;font-weight:600;">Order Total</td>
                                <td align="right" style="font-size:22px;font-weight:800;color:#ff9a3c;letter-spacing:-0.3px;">{total.Amount:F2} <span style="font-size:14px;font-weight:600;color:#7a94b8;">{total.Currency}</span></td>
                              </tr>
                            </table>
                          </td>
                        </tr>
                      </table>

                      <!-- CTA -->
                      <table cellpadding="0" cellspacing="0" border="0" style="margin:0 auto;">
                        <tr>
                          <td style="background:linear-gradient(135deg,#c45c00,#e87722);border-radius:999px;">
                            <a href="{_clientBaseUrl}/orders/{orderId}" style="display:inline-block;padding:15px 44px;background:linear-gradient(135deg,#c45c00,#e87722);color:#fff;font-size:15px;font-weight:700;text-decoration:none;border-radius:999px;letter-spacing:0.02em;">View My Order</a>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>

                  <!-- Footer -->
                  <tr>
                    <td style="background:#071428;padding:22px 48px;border-top:1px solid #1c3868;text-align:center;">
                      <p style="margin:0 0 4px;font-size:12px;color:#5a7498;">© {DateTime.UtcNow.Year} MarketCore. All rights reserved.</p>
                      <p style="margin:0;font-size:11px;color:#3a5478;">This is an automated message — please do not reply.</p>
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
