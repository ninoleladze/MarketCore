using MarketCore.Domain.Entities;

namespace MarketCore.Domain.Repositories;

public interface IOrderRepository : IRepository<Order>
{

    Task<IEnumerable<Order>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default);

    Task<decimal> GetTotalRevenueAsync(CancellationToken ct = default);
}
