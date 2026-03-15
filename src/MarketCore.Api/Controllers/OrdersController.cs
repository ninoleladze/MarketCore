using Asp.Versioning;
using MarketCore.Application.Features.Orders.Commands.CancelOrder;
using MarketCore.Application.Features.Orders.Commands.Checkout;
using MarketCore.Application.Features.Orders.Commands.UpdateOrderStatus;
using MarketCore.Application.Features.Orders.Queries.GetOrderById;
using MarketCore.Application.Features.Orders.Queries.GetUserOrders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketCore.Api.Controllers;

[ApiVersion("1.0")]
[Authorize]
public sealed class OrdersController : BaseApiController
{

    [HttpPost("checkout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Checkout(
        [FromBody] CheckoutCommand command,
        CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserOrders(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetUserOrdersQuery(), ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetOrderByIdQuery(id), ct);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CancelOrder(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new CancelOrderCommand(id), ct);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateOrderStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken ct)
    {
        var command = new UpdateOrderStatusCommand(id, request.NewStatus);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}

public sealed record UpdateOrderStatusRequest(string NewStatus);
