using MarketCore.Domain.Repositories;

namespace MarketCore.Application.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{

    IProductRepository Products { get; }

    IOrderRepository Orders { get; }

    IUserRepository Users { get; }

    ICartRepository Carts { get; }

    ICategoryRepository Categories { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task BeginTransactionAsync(CancellationToken ct = default);

    Task CommitAsync(CancellationToken ct = default);

    Task RollbackAsync(CancellationToken ct = default);
}
