using Asp.Versioning;
using MarketCore.Application.Features.Auth.Commands.ForgotPassword;
using MarketCore.Application.Features.Auth.Commands.Login;
using MarketCore.Application.Features.Auth.Commands.Register;
using MarketCore.Application.Features.Auth.Commands.ResetPassword;
using MarketCore.Application.Features.Auth.Commands.VerifyEmail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MarketCore.Api.Controllers;

[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class AuthController : BaseApiController
{

    [HttpPost("register")]
    [EnableRateLimiting("AuthSliding")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterCommand command,
        CancellationToken ct)
    {

        var origin = Request.Headers.Origin.FirstOrDefault() ?? "http://localhost:4200";
        var commandWithOrigin = command with { ClientBaseUrl = origin };
        var result = await Mediator.Send(commandWithOrigin, ct);
        return ToActionResult(result);
    }

    [HttpGet("verify-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token, CancellationToken ct)
    {
        var result = await Mediator.Send(new VerifyEmailCommand(token), ct);
        return ToActionResult(result);
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthSliding")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken ct)
    {
        var origin = Request.Headers.Origin.FirstOrDefault() ?? "http://localhost:4200";
        var commandWithOrigin = command with { ClientBaseUrl = origin };
        var result = await Mediator.Send(commandWithOrigin, ct);
        return ToActionResult(result);
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}
