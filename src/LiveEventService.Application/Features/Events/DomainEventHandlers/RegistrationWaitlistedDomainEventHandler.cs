using MediatR;
using Microsoft.Extensions.Logging;
using LiveEventService.Core.Common;
using LiveEventService.Application.Common.Notifications;

namespace LiveEventService.Application.Features.Events.DomainEventHandlers;

public class RegistrationWaitlistedDomainEventHandler
    : INotificationHandler<RegistrationWaitlistedNotification>
{
    private readonly ILogger<RegistrationWaitlistedDomainEventHandler> _logger;
    private readonly IRepository<Core.Events.Event> _eventRepository;

    public RegistrationWaitlistedDomainEventHandler(
        ILogger<RegistrationWaitlistedDomainEventHandler> logger,
        IRepository<Core.Events.Event> eventRepository)
    {
        _logger = logger;
        _eventRepository = eventRepository;
    }

    public async Task Handle(
        RegistrationWaitlistedNotification notification,
        CancellationToken cancellationToken)
    {
        var registration = notification.DomainEvent.Registration;

        // If position isn't set, calculate it
        if (!registration.PositionInQueue.HasValue)
        {
            var @event = await _eventRepository.GetByIdAsync(registration.EventId);
            if (@event == null)
            {
                _logger.LogError(
                    "Event {EventId} not found for waitlisted registration {RegistrationId}",
                    registration.EventId, registration.Id);
                return;
            }

            var position = @event.GetNextWaitlistPosition();
            registration.UpdateWaitlistPosition(position);

            _logger.LogInformation(
                "Assigned position {Position} to waitlisted registration {RegistrationId} for event {EventId}",
                position, registration.Id, registration.EventId);

            await _eventRepository.UpdateAsync(@event);
        }
    }
}
