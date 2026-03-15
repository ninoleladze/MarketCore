using MarketCore.Domain.Entities;
using MarketCore.Domain.Repositories;
using MarketCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarketCore.Infrastructure.Repositories;

public sealed class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Category?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var normalised = name.Trim().ToLowerInvariant();

        return await Context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name.ToLower() == normalised, ct);
    }

    public async Task<IEnumerable<Category>> GetAllWithProductCountAsync(
        CancellationToken ct = default)
    {
        return await Context.Categories
            .Include(c => c.Products)
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }
}
