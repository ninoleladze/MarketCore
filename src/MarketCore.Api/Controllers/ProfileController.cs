using Asp.Versioning;
using MarketCore.Application.Features.Profile.Queries.GetProfile;
using MarketCore.Application.Features.Profile.Commands.UpdateProfile;
using MarketCore.Application.Features.Profile.Commands.ChangePassword;
using MarketCore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketCore.Api.Controllers;

[ApiVersion("1.0")]
[Authorize]
public sealed class ProfileController : BaseApiController
{
    private readonly ICurrentUserService _currentUser;

    public ProfileController(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId)
            return Unauthorized(new { error = "Not authenticated." });
        return ToActionResult(await Mediator.Send(new GetProfileQuery(userId), ct));
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId)
            return Unauthorized(new { error = "Not authenticated." });
        return ToActionResult(await Mediator.Send(new UpdateProfileCommand(
            userId,
            request.FirstName,
            request.LastName,
            request.GitHubUrl,
            request.Street,
            request.City,
            request.State,
            request.ZipCode,
            request.Country), ct));
    }

    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId)
            return Unauthorized(new { error = "Not authenticated." });
        return ToActionResult(await Mediator.Send(new ChangePasswordCommand(
            userId,
            request.CurrentPassword,
            request.NewPassword), ct));
    }
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
