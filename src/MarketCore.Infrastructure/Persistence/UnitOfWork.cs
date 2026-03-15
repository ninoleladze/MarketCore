using MarketCore.Application.Exceptions;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Repositories;
using MarketCore.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MarketCore.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    private IProductRepository? _products;
    private IOrderRepository? _orders;
    private IUserRepository? _users;
    private ICartRepository? _carts;
    private ICategoryRepository? _categories;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IProductRepository Products =>
        _products ??= new ProductRepository(_context);

    public IOrderRepository Orders =>
        _orders ??= new OrderRepository(_context);

    public IUserRepository Users =>
        _users ??= new UserRepository(_context);

    public ICartRepository Carts =>
        _carts ??= new CartRepository(_context);

    public ICategoryRepository Categories =>
        _categories ??= new CategoryRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {

            var entityName = ex.Entries.FirstOrDefault()?.Entity.GetType().Name ?? "Entity";

            var fieldName = ExtractFieldHint(ex);

            throw new DuplicateKeyException(
                entityName,
                fieldName,
                $"A record with the same {fieldName} already exists.",
                ex);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        if (inner is null) return false;

        var message = inner.Message;

        if (message.Contains("2627", StringComparison.Ordinal) ||
            message.Contains("2601", StringComparison.Ordinal))
        {
            return true;
        }

        if (message.Contains("23505", StringComparison.Ordinal))
            return true;

        if (message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("UNIQUE KEY", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static string ExtractFieldHint(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? string.Empty;

        var ixIndex = message.IndexOf("IX_", StringComparison.OrdinalIgnoreCase);
        if (ixIndex >= 0)
        {
            var segment = message[ixIndex..];

            var lastUnderscore = segment.LastIndexOf('_');
            if (lastUnderscore >= 0 && lastUnderscore < segment.Length - 1)
            {
                var candidate = new string(
                    segment[(lastUnderscore + 1)..]
                        .TakeWhile(c => char.IsLetterOrDigit(c) || c == '_')
                        .ToArray());

                if (!string.IsNullOrEmpty(candidate))
                    return candidate;
            }
        }

        var keyIndex = message.IndexOf("Key (", StringComparison.OrdinalIgnoreCase);
        if (keyIndex >= 0)
        {
            var start = keyIndex + 5;
            var end = message.IndexOf(')', start);
            if (end > start)
                return message[start..end];
        }

        return "value";
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction is not null)
            throw new InvalidOperationException(
                "A transaction is already in progress. Commit or roll back before beginning a new one.");

        _currentTransaction = await _context.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_currentTransaction is null)
            throw new InvalidOperationException(
                "No active transaction to commit. Call BeginTransactionAsync first.");

        try
        {
            await _currentTransaction.CommitAsync(ct);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_currentTransaction is null)
            return;

        try
        {
            await _currentTransaction.RollbackAsync(ct);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction is not null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }

        await _context.DisposeAsync();
    }
}
