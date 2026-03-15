using Asp.Versioning;
using MarketCore.Application.Features.Cart.Commands.AddToCart;
using MarketCore.Application.Features.Cart.Commands.ClearCart;
using MarketCore.Application.Features.Cart.Commands.RemoveFromCart;
using MarketCore.Application.Features.Cart.Queries.GetCart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketCore.Api.Controllers;

[ApiVersion("1.0")]
[Authorize]
public sealed class CartController : BaseApiController
{

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCart(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCartQuery(), ct);
        return ToActionResult(result);
    }

    [HttpPost("items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddToCart(
        [FromBody] AddToCartCommand command,
        CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("items/{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RemoveFromCart(Guid productId, CancellationToken ct)
    {
        var result = await Mediator.Send(new RemoveFromCartCommand(productId), ct);
        return ToActionResult(result);
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClearCart(CancellationToken ct)
    {
        var result = await Mediator.Send(new ClearCartCommand(), ct);
        return ToActionResult(result);
    }
}
