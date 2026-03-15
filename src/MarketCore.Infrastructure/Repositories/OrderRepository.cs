using MarketCore.Domain.Entities;
using MarketCore.Domain.Repositories;
using MarketCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarketCore.Infrastructure.Repositories;

public sealed class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Order>> GetByUserIdAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        return await Context.Orders
            .Include(o => o.OrderItems)
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default)
    {
        return await Context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<decimal> GetTotalRevenueAsync(CancellationToken ct = default)
    {
        return await Context.Orders
            .AsNoTracking()
            .SumAsync(o => (decimal?)o.TotalAmount.Amount, ct) ?? 0m;
    }
}
