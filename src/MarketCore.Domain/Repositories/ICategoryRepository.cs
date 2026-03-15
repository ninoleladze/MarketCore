using MarketCore.Domain.Entities;

namespace MarketCore.Domain.Repositories;

public interface ICategoryRepository : IRepository<Category>
{

    Task<Category?> GetByNameAsync(string name, CancellationToken ct = default);

    Task<IEnumerable<Category>> GetAllWithProductCountAsync(CancellationToken ct = default);
}
