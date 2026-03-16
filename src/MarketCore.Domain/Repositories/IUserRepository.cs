using MarketCore.Domain.Entities;

namespace MarketCore.Domain.Repositories;

public interface IUserRepository : IRepository<User>
{

    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<bool> ExistsAsync(string email, CancellationToken ct = default);

    Task<User?> GetByVerificationTokenAsync(string token, CancellationToken ct = default);
}
