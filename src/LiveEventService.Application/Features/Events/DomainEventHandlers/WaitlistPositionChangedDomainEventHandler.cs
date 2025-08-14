using MediatR;
using Microsoft.Extensions.Logging;
using LiveEventService.Core.Common;
using LiveEventService.Application.Common.Notifications;
using LiveEventService.Application.Common;

namespace LiveEventService.Application.Features.Events.DomainEventHandlers;

[AsyncProcessing(Priority = 1, MaxRetryAttempts = 3, RetryDelaySeconds = 2)]
public class WaitlistPositionChangedDomainEventHandler
    : INotificationHandler<WaitlistPositionChangedNotification>
{
    private readonly ILogger<WaitlistPositionChangedDomainEventHandler> _logger;
    private readonly IRepository<Core.Events.Event> _eventRepository;

    public WaitlistPositionChangedDomainEventHandler(
        ILogger<WaitlistPositionChangedDomainEventHandler> logger,
        IRepository<Core.Events.Event> eventRepository)
    {
        _logger = logger;
        _eventRepository = eventRepository;
    }

    public Task Handle(
        WaitlistPositionChangedNotification notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Waitlist position updated for registration {RegistrationId} in event {EventId}: {OldPosition} -> {NewPosition}",
            notification.DomainEvent.RegistrationId,
            notification.DomainEvent.EventId,
            notification.DomainEvent.OldPosition?.ToString() ?? "null",
            notification.DomainEvent.NewPosition?.ToString() ?? "null");

        // Here you could add additional logic like:
        // - Notify the user about their new position
        // - Update any read models or projections
        // - Trigger notifications for significant position changes

        // Example: Notify user if they moved into the top 5 positions
        if (notification.DomainEvent.NewPosition <= 5 && (notification.DomainEvent.OldPosition == null || notification.DomainEvent.OldPosition > 5))
        {
            _logger.LogInformation(
                "Registration {RegistrationId} is now in the top 5 waitlist positions for event {EventId}",
                notification.DomainEvent.RegistrationId, notification.DomainEvent.EventId);
        }

        return Task.CompletedTask;
    }
}
