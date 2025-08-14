using LiveEventService.Application.Common.Notifications;
using LiveEventService.Core.Common;
using LiveEventService.Core.Registrations.EventRegistration;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LiveEventService.Application.Features.Events.DomainEventHandlers;

public class EventRegistrationCancelledDomainEventHandler : INotificationHandler<EventRegistrationCancelledNotification>
{
    private readonly IEventRegistrationNotifier _notifier;
    private readonly IRepository<Core.Registrations.EventRegistration.EventRegistration> _registrationRepository;
    private readonly IRepository<Core.Events.Event> _eventRepository;
    private readonly ILogger<EventRegistrationCancelledDomainEventHandler> _logger;

    public EventRegistrationCancelledDomainEventHandler(
        IEventRegistrationNotifier notifier,
        IRepository<Core.Registrations.EventRegistration.EventRegistration> registrationRepository,
        IRepository<Core.Events.Event> eventRepository,
        ILogger<EventRegistrationCancelledDomainEventHandler> logger)
    {
        _notifier = notifier;
        _registrationRepository = registrationRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task Handle(EventRegistrationCancelledNotification notification, CancellationToken cancellationToken)
    {
        var registration = notification.DomainEvent.Registration;

        // Notify about the cancellation
        await _notifier.NotifyAsync(registration, "cancelled", cancellationToken);

        // Only promote waitlisted registrations if this was a confirmed registration that was cancelled
        // We can determine this by checking if there are more confirmed registrations than the event capacity
        var confirmedRegistrations = await _registrationRepository.ListAsync(
            new ConfirmedRegistrationsForEventSpecification(registration.EventId),
            cancellationToken);

        var waitlisted = await _registrationRepository.ListAsync(
            new WaitlistedRegistrationsForEventSpecification(registration.EventId),
            cancellationToken);

        // Get the event to check its capacity
        var @event = await _eventRepository.GetByIdAsync(registration.EventId, cancellationToken);
        if (@event == null)
        {
            _logger.LogWarning("Event {EventId} not found when processing cancellation", registration.EventId);
            return;
        }

        // If we have fewer confirmed registrations than capacity and there are waitlisted registrations,
        // then we can promote someone
        if (confirmedRegistrations.Count < @event.Capacity && waitlisted.Any())
        {
            var promote = waitlisted.First();
            promote.Confirm();
            await _registrationRepository.UpdateAsync(promote, cancellationToken);

            _logger.LogInformation(
                "Promoted registration {RegistrationId} from waitlist after cancellation of confirmed registration {CancelledRegistrationId}",
                promote.Id, registration.Id);

            // Update positions for remaining waitlisted registrations
            for (int i = 0; i < waitlisted.Skip(1).Count(); i++)
            {
                var waitlistedRegistration = waitlisted.Skip(1).ElementAt(i);
                var newPosition = i + 1;

                if (waitlistedRegistration.PositionInQueue != newPosition)
                {
                    waitlistedRegistration.UpdateWaitlistPosition(newPosition);
                    await _registrationRepository.UpdateAsync(waitlistedRegistration, cancellationToken);

                    _logger.LogInformation(
                        "Updated waitlist position for registration {RegistrationId} from {OldPosition} to {NewPosition}",
                        waitlistedRegistration.Id, waitlistedRegistration.PositionInQueue, newPosition);
                }
            }
        }
    }
}

// Specification to get confirmed registrations for an event
public class ConfirmedRegistrationsForEventSpecification : BaseSpecification<Core.Registrations.EventRegistration.EventRegistration>
{
    public ConfirmedRegistrationsForEventSpecification(Guid eventId)
    {
        Criteria = r => r.EventId == eventId && r.Status == RegistrationStatus.Confirmed;
    }
}
