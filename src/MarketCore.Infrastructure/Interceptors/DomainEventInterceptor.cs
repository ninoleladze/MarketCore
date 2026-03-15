using MarketCore.Application.DomainEventHandlers;
using MarketCore.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace MarketCore.Infrastructure.Interceptors;

public sealed class DomainEventInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {

        DispatchDomainEventsAsync(eventData.Context!, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        return base.SavedChanges(eventData, result);
    }

    private async Task DispatchDomainEventsAsync(
        Microsoft.EntityFrameworkCore.DbContext context,
        CancellationToken cancellationToken)
    {

        var aggregatesWithEvents = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (aggregatesWithEvents.Count == 0)
            return;

        var domainEvents = aggregatesWithEvents
            .SelectMany(a => a.DomainEvents)
            .ToList();

        foreach (var aggregate in aggregatesWithEvents)
            aggregate.ClearDomainEvents();

        await using var scope = _serviceProvider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        foreach (var domainEvent in domainEvents)
        {
            var notificationType = typeof(DomainEventNotification<>)
                .MakeGenericType(domainEvent.GetType());

            var notification = (INotification)Activator.CreateInstance(
                notificationType, domainEvent)!;

            await publisher.Publish(notification, cancellationToken);
        }
    }
}
