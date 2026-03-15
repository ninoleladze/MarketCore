using MarketCore.Domain.Entities;

namespace MarketCore.Domain.Repositories;

public interface ICartRepository : IRepository<Cart>
{

    Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<Cart?> GetByUserIdWithItemsAsync(Guid userId, CancellationToken ct = default);
}
