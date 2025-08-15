using HotChocolate.Subscriptions;
using LiveEventService.Core.Common;
using LiveEventService.Core.Registrations.EventRegistration;

namespace LiveEventService.API.Events;

/// <summary>
/// Notifies GraphQL subscribers about event registration changes.
/// Implements the domain event notification pattern for real-time updates.
/// </summary>
public class EventRegistrationNotifier : IEventRegistrationNotifier
{
    private readonly ITopicEventSender eventSender;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventRegistrationNotifier"/> class.
    /// </summary>
    /// <param name="eventSender">The HotChocolate topic event sender for GraphQL subscriptions.</param>
    public EventRegistrationNotifier(ITopicEventSender eventSender)
    {
        this.eventSender = eventSender;
    }

    /// <summary>
    /// Sends a notification to GraphQL subscribers about an event registration change.
    /// Creates a topic-specific notification with user and event details.
    /// </summary>
    /// <param name="reg">The event registration that changed.</param>
    /// <param name="action">The action that occurred (e.g., "registered", "cancelled").</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous notification operation.</returns>
    public async Task NotifyAsync(EventRegistration reg, string action, CancellationToken cancellationToken = default)
    {
        var eventId = reg.EventId;
        var topic = $"eventRegistration_{eventId}";
        var userName = reg.User != null ? $"{reg.User.FirstName} {reg.User.LastName}".Trim() : string.Empty;
        var payload = new EventRegistrationNotification
        {
            EventId = eventId,
            EventTitle = reg.Event?.Name ?? string.Empty,
            UserId = reg.UserId.ToString(),
            UserName = userName,
            Action = action,
            Timestamp = DateTime.UtcNow
        };
        await eventSender.SendAsync(topic, payload, cancellationToken);
    }
}
