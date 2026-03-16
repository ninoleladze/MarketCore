using MarketCore.Domain.Entities;
using MarketCore.Domain.Repositories;
using MarketCore.Domain.ValueObjects;
using MarketCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarketCore.Infrastructure.Repositories;

public sealed class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var emailObj = new Email(email.Trim().ToLowerInvariant());
        return await Context.Users
            .FirstOrDefaultAsync(u => u.Email == emailObj, ct);
    }

    public async Task<bool> ExistsAsync(string email, CancellationToken ct = default)
    {
        var emailObj = new Email(email.Trim().ToLowerInvariant());
        return await Context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == emailObj, ct);
    }

    public async Task<User?> GetByVerificationTokenAsync(string token, CancellationToken ct = default)
    {
        return await Context.Users
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token, ct);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await Context.Users
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }
}
