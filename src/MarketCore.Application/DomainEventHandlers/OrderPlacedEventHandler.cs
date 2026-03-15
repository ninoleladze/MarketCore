using MediatR;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MarketCore.Application.DomainEventHandlers;

public sealed class OrderPlacedEventHandler
    : INotificationHandler<DomainEventNotification<OrderPlacedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<OrderPlacedEventHandler> _logger;

    public OrderPlacedEventHandler(
        IEmailService emailService,
        IUnitOfWork uow,
        ILogger<OrderPlacedEventHandler> logger)
    {
        _emailService = emailService;
        _uow = uow;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<OrderPlacedEvent> notification,
        CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;

        _logger.LogInformation(
            "Order {OrderId} placed by user {UserId} for {TotalAmount}. Sending confirmation email.",
            evt.OrderId, evt.UserId, evt.TotalAmount);

        var user = await _uow.Users.GetByIdAsync(evt.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning(
                "OrderPlacedEventHandler: User {UserId} not found — cannot send confirmation email for Order {OrderId}.",
                evt.UserId, evt.OrderId);
            return;
        }

        try
        {
            await _emailService.SendOrderConfirmationAsync(
                user.Email.Value,
                evt.OrderId,
                evt.TotalAmount,
                cancellationToken);

            _logger.LogInformation(
                "Confirmation email sent to {Email} for Order {OrderId}.",
                user.Email.Value, evt.OrderId);
        }
        catch (Exception ex)
        {

            _logger.LogWarning(
                ex,
                "Failed to send confirmation email to {Email} for Order {OrderId}. Email will not be retried in this request.",
                user.Email.Value, evt.OrderId);
        }
    }
}
