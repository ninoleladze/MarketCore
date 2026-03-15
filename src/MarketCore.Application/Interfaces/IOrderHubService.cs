namespace MarketCore.Application.Interfaces;

public interface IOrderHubService
{

    Task NotifyOrderStatusChangedAsync(
        string orderId,
        string userId,
        string newStatus,
        CancellationToken ct = default);

    Task NotifyNewOrderAsync(
        string userId,
        string orderId,
        decimal totalAmount,
        CancellationToken ct = default);
}
