using Asp.Versioning;
using MarketCore.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketCore.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    private IMediator? _mediator;

    protected IMediator Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        var error = result.Error ?? "An error occurred.";

        if (IsNotFound(error))
            return NotFound(new { error });

        if (IsUnauthorized(error))
            return Unauthorized(new { error });

        return UnprocessableEntity(new { error });
    }

    protected ActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
            return NoContent();

        var error = result.Error ?? "An error occurred.";

        if (IsNotFound(error))
            return NotFound(new { error });

        if (IsUnauthorized(error))
            return Unauthorized(new { error });

        return UnprocessableEntity(new { error });
    }

    private static bool IsNotFound(string error) =>
        error.Contains("not found", StringComparison.OrdinalIgnoreCase);

    private static bool IsUnauthorized(string error) =>
        error.Contains("not authoris", StringComparison.OrdinalIgnoreCase) ||
        error.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
        error.Contains("not authenticated", StringComparison.OrdinalIgnoreCase);
}
