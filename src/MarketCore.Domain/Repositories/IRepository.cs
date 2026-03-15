using MarketCore.Domain.Common;

namespace MarketCore.Domain.Repositories;

public interface IRepository<T> where T : BaseEntity
{

    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);

    Task AddAsync(T entity, CancellationToken ct = default);

    void Update(T entity);

    void Delete(T entity);

    Task<int> CountAllAsync(CancellationToken ct = default);
}
