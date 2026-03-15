using Asp.Versioning;
using MarketCore.Application.Features.Reviews.Commands.AddReview;
using MarketCore.Application.Features.Reviews.Queries.GetProductReviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketCore.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products/{productId:guid}/reviews")]
public sealed class ReviewsController : BaseApiController
{

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductReviews(Guid productId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetProductReviewsQuery(productId), ct);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddReview(
        Guid productId,
        [FromBody] AddReviewRequest request,
        CancellationToken ct)
    {
        var command = new AddReviewCommand(productId, request.Rating, request.Comment);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}

public sealed record AddReviewRequest(int Rating, string Comment);
