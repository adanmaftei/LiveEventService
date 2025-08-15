using LiveEventService.Core.Common;

namespace LiveEventService.Core.Events;

/// <summary>
/// Domain event raised when an event's capacity is increased.
/// </summary>
public class EventCapacityIncreasedDomainEvent : DomainEvent
{
    /// <summary>Gets the event whose capacity increased.</summary>
    public Event Event { get; }

    /// <summary>Gets the amount by which capacity increased.</summary>
    public int AdditionalCapacity { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventCapacityIncreasedDomainEvent"/> class.
    /// Creates a new instance with the affected event and the increase amount.
    /// </summary>
    /// <param name="event">The event whose capacity was increased.</param>
    /// <param name="additionalCapacity">The amount by which the capacity was increased.</param>
    public EventCapacityIncreasedDomainEvent(Event @event, int additionalCapacity)
    {
        Event = @event ?? throw new ArgumentNullException(nameof(@event));
        AdditionalCapacity = additionalCapacity;
    }
}
