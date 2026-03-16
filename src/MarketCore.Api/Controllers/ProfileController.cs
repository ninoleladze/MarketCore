using Asp.Versioning;
using MarketCore.Application.Features.Profile.Queries.GetProfile;
using MarketCore.Application.Features.Profile.Commands.UpdateProfile;
using MarketCore.Application.Features.Profile.Commands.ChangePassword;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MarketCore.Api.Controllers;

[ApiVersion("1.0")]
[Authorize]
public sealed class ProfileController : BaseApiController
{
    // JWT uses JwtRegisteredClaimNames.Sub for user ID;
    // ASP.NET Core maps "sub" → ClaimTypes.NameIdentifier automatically.
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new GetProfileQuery(CurrentUserId), ct));

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request, CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new UpdateProfileCommand(
            CurrentUserId,
            request.FirstName,
            request.LastName,
            request.GitHubUrl,
            request.Street,
            request.City,
            request.State,
            request.ZipCode,
            request.Country), ct));

    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request, CancellationToken ct) =>
        ToActionResult(await Mediator.Send(new ChangePasswordCommand(
            CurrentUserId,
            request.CurrentPassword,
            request.NewPassword), ct));
}

public sealed record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string? GitHubUrl,
    string? Street,
    string? City,
    string? State,
    string? ZipCode,
    string? Country);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);
