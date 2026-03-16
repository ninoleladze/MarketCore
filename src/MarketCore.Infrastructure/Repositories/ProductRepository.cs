using MarketCore.Domain.Entities;
using MarketCore.Domain.Repositories;
using MarketCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarketCore.Infrastructure.Repositories;

public sealed class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context) { }

    public async Task<(IEnumerable<Product> Items, int TotalCount)> SearchAsync(
        string? term,
        Guid? categoryId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = Context.Products
            .Include(p => p.Category)
            .Include(p => p.Reviews)
            .AsNoTracking()
            .Where(p => p.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(term))
        {
            var normalised = term.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(normalised) ||
                p.Description.ToLower().Contains(normalised));
        }

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        query = query.OrderBy(p => p.Name);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<Product?> GetByIdWithReviewsAsync(Guid id, CancellationToken ct = default)
    {
        return await Context.Products
            .Include(p => p.Reviews)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, ct);
    }

    public async Task<int> CountByCategoryAsync(Guid categoryId, CancellationToken ct = default)
    {
        return await Context.Products.CountAsync(p => p.CategoryId == categoryId, ct);
    }
}
