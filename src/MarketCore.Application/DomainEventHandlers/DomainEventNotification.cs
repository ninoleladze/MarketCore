using MediatR;
using MarketCore.Domain.Common;

namespace MarketCore.Application.DomainEventHandlers;

public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent)
    : INotification
    where TDomainEvent : IDomainEvent;
