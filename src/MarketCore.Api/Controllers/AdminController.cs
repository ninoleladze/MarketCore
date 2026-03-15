using Asp.Versioning;
using MarketCore.Application.Features.Admin.Queries.GetAdminStats;
using MarketCore.Application.Features.Admin.Queries.GetAdminOrders;
using MarketCore.Application.Features.Orders.Commands.UpdateOrderStatus;
using MarketCore.Application.Features.Products.Queries.GetProducts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketCore.Api.Controllers;

[ApiVersion("1.0")]
[Authorize(Roles = "Admin")]
public sealed class AdminController : BaseApiController
{

    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetAdminStatsQuery(), ct);
        return ToActionResult(result);
    }

    [HttpGet("products")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new GetProductsQuery(searchTerm, categoryId, page, pageSize);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("orders")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new GetAdminOrdersQuery(page, pageSize);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPatch("orders/{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateOrderStatus(
        Guid id,
        [FromBody] AdminUpdateOrderStatusRequest request,
        CancellationToken ct)
    {
        var command = new UpdateOrderStatusCommand(id, request.NewStatus);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}

public sealed record AdminUpdateOrderStatusRequest(string NewStatus);
