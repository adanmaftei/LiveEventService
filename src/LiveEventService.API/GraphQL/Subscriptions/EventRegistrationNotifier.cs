using LiveEventService.Core.Registrations.EventRegistration;
using HotChocolate.Subscriptions;
using LiveEventService.Application.Features.Events.EventRegistration.Notifications;

namespace LiveEventService.API.Events;

public class EventRegistrationNotifier : IEventRegistrationNotifier
{
    private readonly ITopicEventSender _eventSender;
    public EventRegistrationNotifier(ITopicEventSender eventSender)
    {
        _eventSender = eventSender;
    }
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
        await _eventSender.SendAsync(topic, payload, cancellationToken);
    }
} 