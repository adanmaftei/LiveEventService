using HotChocolate.Subscriptions;
using HotChocolate.Execution;
using LiveEventService.Application.Features.Events.Event;

namespace LiveEventService.API.Events;

[ExtendObjectType(OperationTypeNames.Subscription)]
public class EventSubscriptions
{
    [Subscribe]
    [Topic("eventCreated")]
    public EventDto OnEventCreated(
        [EventMessage] EventDto eventDto) => eventDto;

    [Subscribe]
    [Topic("eventUpdated_{eventId}")]
    public EventDto OnEventUpdated(
        [EventMessage] EventDto eventDto) => eventDto;

    [Subscribe]
    [Topic("eventRegistration_{eventId}")]
    public EventRegistrationNotification OnEventRegistration(
        [EventMessage] EventRegistrationNotification notification) => notification;

    [Subscribe(With = nameof(SubscribeToEventRegistrations))]
    public EventRegistrationNotification OnEventRegistrationByEventId(
        [EventMessage] EventRegistrationNotification notification,
        Guid eventId) => notification;

    public static async ValueTask<ISourceStream<EventRegistrationNotification>> SubscribeToEventRegistrations(
        Guid eventId,
        [Service] ITopicEventReceiver eventReceiver,
        CancellationToken cancellationToken)
    {
        var topic = $"eventRegistration_{eventId}";
        return await eventReceiver.SubscribeAsync<EventRegistrationNotification>(topic, cancellationToken);
    }
}

public class EventRegistrationNotification
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // "registered", "cancelled", etc.
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
