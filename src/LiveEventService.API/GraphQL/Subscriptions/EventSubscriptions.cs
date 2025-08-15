using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using LiveEventService.Application.Features.Events.Event;

namespace LiveEventService.API.Events;

/// <summary>
/// GraphQL subscriptions for real-time event updates.
/// Provides live notifications for event creation, updates, and registration changes.
/// </summary>
[ExtendObjectType(OperationTypeNames.Subscription)]
public class EventSubscriptions
{
    /// <summary>
    /// Creates a subscription stream for event registration notifications.
    /// Used internally by HotChocolate to manage subscription topics.
    /// </summary>
    /// <param name="eventId">The ID of the event to subscribe to.</param>
    /// <param name="eventReceiver">The HotChocolate topic event receiver.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A source stream for event registration notifications.</returns>
    public static ValueTask<ISourceStream<EventRegistrationNotification>> SubscribeToEventRegistrations(
        Guid eventId,
        [Service] ITopicEventReceiver eventReceiver,
        CancellationToken cancellationToken)
    {
        var topic = $"eventRegistration_{eventId}";
        return eventReceiver.SubscribeAsync<EventRegistrationNotification>(topic, cancellationToken);
    }

    /// <summary>
    /// Subscribes to event creation notifications.
    /// Receives notifications when new events are created in the system.
    /// </summary>
    /// <param name="eventDto">The newly created event data.</param>
    /// <returns>The event data for the subscription.</returns>
    [Subscribe]
    [Topic("eventCreated")]
    public EventDto OnEventCreated(
        [EventMessage] EventDto eventDto) => eventDto;

    /// <summary>
    /// Subscribes to event update notifications for a specific event.
    /// Receives notifications when an event is updated.
    /// </summary>
    /// <param name="eventDto">The updated event data.</param>
    /// <returns>The event data for the subscription.</returns>
    [Subscribe]
    [Topic("eventUpdated_{eventId}")]
    public EventDto OnEventUpdated(
        [EventMessage] EventDto eventDto) => eventDto;

    /// <summary>
    /// Subscribes to event registration notifications for a specific event.
    /// Receives notifications when users register or cancel registrations.
    /// </summary>
    /// <param name="notification">The registration notification data.</param>
    /// <returns>The notification data for the subscription.</returns>
    [Subscribe]
    [Topic("eventRegistration_{eventId}")]
    public EventRegistrationNotification OnEventRegistration(
        [EventMessage] EventRegistrationNotification notification) => notification;

    /// <summary>
    /// Subscribes to event registration notifications with dynamic event ID filtering.
    /// Uses a custom subscription resolver to filter by specific event ID.
    /// </summary>
    /// <param name="notification">The registration notification data.</param>
    /// <param name="eventId">The event ID to filter notifications for.</param>
    /// <returns>The notification data for the subscription.</returns>
    [Subscribe(With = nameof(SubscribeToEventRegistrations))]
    public EventRegistrationNotification OnEventRegistrationByEventId(
        [EventMessage] EventRegistrationNotification notification,
        Guid eventId) => notification;
}

/// <summary>
/// Represents a notification about an event registration change.
/// Used for real-time updates in GraphQL subscriptions.
/// </summary>
public class EventRegistrationNotification
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // "registered", "cancelled", etc.
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
