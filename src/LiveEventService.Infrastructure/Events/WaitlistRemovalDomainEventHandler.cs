using MediatR;
using LiveEventService.Infrastructure.Events.WaitlistNotifications;
using Microsoft.Extensions.Logging;
using LiveEventService.Core.Common;
using LiveEventService.Core.Events;
using LiveEventService.Core.Registrations.EventRegistration;

namespace LiveEventService.Infrastructure.Events;

public class WaitlistRemovalDomainEventHandler 
    : INotificationHandler<WaitlistRemovalNotification>
{
    private readonly ILogger<WaitlistRemovalDomainEventHandler> _logger;
    private readonly IRepository<Event> _eventRepository;
    private readonly IRepository<EventRegistration> _registrationRepository;

    public WaitlistRemovalDomainEventHandler(
        ILogger<WaitlistRemovalDomainEventHandler> logger,
        IRepository<Event> eventRepository,
        IRepository<EventRegistration> registrationRepository)
    {
        _logger = logger;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
    }

    public async Task Handle(
        WaitlistRemovalNotification notification, 
        CancellationToken cancellationToken)
    {
        var registration = notification.DomainEvent.Registration;
        
        // Log the removal reason if provided
        if (!string.IsNullOrEmpty(notification.DomainEvent.Reason))
        {
            _logger.LogInformation(
                "Registration {RegistrationId} removed from waitlist for event {EventId}. Reason: {Reason}",
                registration.Id, registration.EventId, notification.DomainEvent.Reason);
        }
        else
        {
            _logger.LogInformation(
                "Registration {RegistrationId} removed from waitlist for event {EventId}",
                registration.Id, registration.EventId);
        }

        // Get all remaining waitlisted registrations for this event
        var remainingWaitlisted = await _registrationRepository.ListAsync(
            new WaitlistedRegistrationsForEventSpecification(registration.EventId),
            cancellationToken);
        
        // Update positions for remaining waitlisted registrations
        for (int i = 0; i < remainingWaitlisted.Count; i++)
        {
            var waitlistedRegistration = remainingWaitlisted[i];
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
        
        _logger.LogInformation(
            "Updated waitlist positions after removal of registration {RegistrationId} from event {EventId}",
            registration.Id, registration.EventId);
    }
}

// Specification to get waitlisted registrations for an event
public class WaitlistedRegistrationsForEventSpecification : BaseSpecification<EventRegistration>
{
    public WaitlistedRegistrationsForEventSpecification(Guid eventId)
    {
        Criteria = r => r.EventId == eventId && r.Status == RegistrationStatus.Waitlisted;
        ApplyOrderBy(r => r.PositionInQueue);
    }
}
