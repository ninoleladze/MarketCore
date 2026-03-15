using MarketCore.Application.Interfaces;
using MarketCore.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace MarketCore.Infrastructure.Services;

public sealed class OrderHubService : IOrderHubService
{
    private readonly IHubContext<OrderHub> _hubContext;
    private readonly ILogger<OrderHubService> _logger;

    public OrderHubService(
        IHubContext<OrderHub> hubContext,
        ILogger<OrderHubService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyOrderStatusChangedAsync(
        string orderId,
        string userId,
        string newStatus,
        CancellationToken ct = default)
    {
        var payload = new
        {
            orderId,
            newStatus,
            updatedAt = DateTime.UtcNow.ToString("o")
        };

        try
        {

            await _hubContext.Clients
                .Group($"order-{orderId}")
                .SendAsync("OrderStatusChanged", payload, ct)
                .ConfigureAwait(false);

            await _hubContext.Clients
                .Group($"user-{userId}")
                .SendAsync("OrderStatusChanged", payload, ct)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "SignalR: OrderStatusChanged sent for Order {OrderId} to user group {UserId}, new status {Status}.",
                orderId, userId, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "SignalR: Failed to send OrderStatusChanged for Order {OrderId}. Client UIs will not update in real time.",
                orderId);
        }
    }

    public async Task NotifyNewOrderAsync(
        string userId,
        string orderId,
        decimal totalAmount,
        CancellationToken ct = default)
    {
        var payload = new
        {
            orderId,
            totalAmount
        };

        try
        {
            await _hubContext.Clients
                .Group($"user-{userId}")
                .SendAsync("NewOrderPlaced", payload, ct)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "SignalR: NewOrderPlaced sent to user group {UserId} for Order {OrderId}.",
                userId, orderId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "SignalR: Failed to send NewOrderPlaced to user group {UserId} for Order {OrderId}.",
                userId, orderId);
        }
    }
}
