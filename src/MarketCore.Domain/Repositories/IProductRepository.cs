using MarketCore.Domain.Entities;

namespace MarketCore.Domain.Repositories;

public interface IProductRepository : IRepository<Product>
{

    Task<(IEnumerable<Product> Items, int TotalCount)> SearchAsync(
        string? term,
        Guid? categoryId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Product?> GetByIdWithReviewsAsync(Guid id, CancellationToken ct = default);

    Task<int> CountByCategoryAsync(Guid categoryId, CancellationToken ct = default);
}
