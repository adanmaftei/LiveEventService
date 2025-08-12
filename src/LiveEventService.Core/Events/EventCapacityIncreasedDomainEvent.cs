using LiveEventService.Core.Common;

namespace LiveEventService.Core.Events;

public class EventCapacityIncreasedDomainEvent : DomainEvent
{
    public Event Event { get; }
    public int AdditionalCapacity { get; }
    
    public EventCapacityIncreasedDomainEvent(Event @event, int additionalCapacity)
    {
        Event = @event ?? throw new ArgumentNullException(nameof(@event));
        AdditionalCapacity = additionalCapacity;
    }
}
