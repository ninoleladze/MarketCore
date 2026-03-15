using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace MarketCore.Infrastructure.Interceptors;

public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public AuditInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            StampAuditFields(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            StampAuditFields(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    private void StampAuditFields(DbContext context)
    {
        var utcNow = DateTime.UtcNow;
        using var scope = _serviceProvider.CreateScope();
        var currentUser = scope.ServiceProvider.GetService<ICurrentUserService>();
        var actor = currentUser?.Email ?? "system";

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreatedAudit(utcNow, actor);
                    break;

                case EntityState.Modified:
                    entry.Entity.SetUpdatedAudit(utcNow);
                    break;
            }
        }
    }
}
