using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace MarketCore.Infrastructure.Hubs;

[Authorize]
public sealed class OrderHub : Hub
{
    private readonly ILogger<OrderHub> _logger;

    public OrderHub(ILogger<OrderHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}").ConfigureAwait(false);
            _logger.LogDebug(
                "OrderHub: Connection {ConnectionId} joined user group {UserId}.",
                Context.ConnectionId, userId);
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug(
            "OrderHub: Connection {ConnectionId} disconnected. Exception: {Message}",
            Context.ConnectionId,
            exception?.Message ?? "none");

        return base.OnDisconnectedAsync(exception);
    }

    public async Task JoinOrderGroup(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}").ConfigureAwait(false);

        _logger.LogDebug(
            "OrderHub: Connection {ConnectionId} joined order group {OrderId}.",
            Context.ConnectionId, orderId);
    }

    public async Task LeaveOrderGroup(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order-{orderId}").ConfigureAwait(false);

        _logger.LogDebug(
            "OrderHub: Connection {ConnectionId} left order group {OrderId}.",
            Context.ConnectionId, orderId);
    }

    public async Task JoinUserGroup()
    {
        var userId = GetUserId();
        if (userId is null)
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}").ConfigureAwait(false);
    }

    private string? GetUserId() =>
        Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Context.User?.FindFirstValue("sub");
}
