using MarketCore.Domain.Entities;
using MarketCore.Domain.Repositories;
using MarketCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarketCore.Infrastructure.Repositories;

public sealed class CartRepository : Repository<Cart>, ICartRepository
{
    public CartRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await Context.Carts
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);
    }

    public async Task<Cart?> GetByUserIdWithItemsAsync(Guid userId, CancellationToken ct = default)
    {
        return await Context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);
    }
}
