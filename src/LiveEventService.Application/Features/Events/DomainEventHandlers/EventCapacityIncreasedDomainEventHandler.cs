using LiveEventService.Core.Common;
using LiveEventService.Core.Registrations.EventRegistration;
using MediatR;
using Microsoft.Extensions.Logging;
using LiveEventService.Application.Common.Notifications;

namespace LiveEventService.Application.Features.Events.DomainEventHandlers;

public class EventCapacityIncreasedDomainEventHandler
    : INotificationHandler<EventCapacityIncreasedNotification>
{
    private readonly ILogger<EventCapacityIncreasedDomainEventHandler> _logger;
    private readonly IRepository<Core.Events.Event> _eventRepository;
    private readonly IRepository<Core.Registrations.EventRegistration.EventRegistration> _registrationRepository;

    public EventCapacityIncreasedDomainEventHandler(
        ILogger<EventCapacityIncreasedDomainEventHandler> logger,
        IRepository<Core.Events.Event> eventRepository,
        IRepository<Core.Registrations.EventRegistration.EventRegistration> registrationRepository)
    {
        _logger = logger;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
    }

    public async Task Handle(
        EventCapacityIncreasedNotification notification,
        CancellationToken cancellationToken)
    {
        var @event = notification.DomainEvent.Event;
        var availableSpots = notification.DomainEvent.AdditionalCapacity;
        var promotedCount = 0;

        _logger.LogInformation(
            "Processing capacity increase for event {EventId}. Additional capacity: {AdditionalCapacity}",
            @event.Id, availableSpots);

        while (availableSpots > 0)
        {
            // Get the next waitlisted registration from the database
            var nextWaitlisted = await _registrationRepository.FirstOrDefaultAsync(
                new NextWaitlistedRegistrationSpecification(@event.Id),
                cancellationToken);

            if (nextWaitlisted == null)
            {
                break;
            }

            // Promote the registration
            nextWaitlisted.Confirm();
            await _registrationRepository.UpdateAsync(nextWaitlisted, cancellationToken);
            availableSpots--;
            promotedCount++;

            _logger.LogInformation(
                "Promoted registration {RegistrationId} from waitlist for event {EventId}",
                nextWaitlisted.Id, @event.Id);
        }

        if (promotedCount > 0)
        {
            // Update positions for remaining waitlisted registrations
            await UpdateRemainingWaitlistPositions(@event.Id, cancellationToken);

            _logger.LogInformation(
                "Promoted {PromotedCount} waitlisted registrations for event {EventId}",
                promotedCount, @event.Id);
        }
    }

    private async Task UpdateRemainingWaitlistPositions(Guid eventId, CancellationToken cancellationToken)
    {
        var remainingWaitlisted = await _registrationRepository.ListAsync(
            new WaitlistedRegistrationsForEventSpecification(eventId),
            cancellationToken);

        for (int i = 0; i < remainingWaitlisted.Count; i++)
        {
            var waitlistedRegistration = remainingWaitlisted[i];
            var newPosition = i + 1;

            if (waitlistedRegistration.PositionInQueue != newPosition)
            {
                waitlistedRegistration.UpdateWaitlistPosition(newPosition);
                await _registrationRepository.UpdateAsync(waitlistedRegistration, cancellationToken);
            }
        }
    }
}

// Specification to get the next waitlisted registration
public class NextWaitlistedRegistrationSpecification : BaseSpecification<Core.Registrations.EventRegistration.EventRegistration>
{
    public NextWaitlistedRegistrationSpecification(Guid eventId)
    {
        Criteria = r => r.EventId == eventId && r.Status == RegistrationStatus.Waitlisted;
        ApplyOrderBy(r => r.PositionInQueue);
        ApplyPaging(0, 1);
    }
}
