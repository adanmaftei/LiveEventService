using LiveEventService.Core.Common;
using EventEntity = LiveEventService.Core.Events.Event;

namespace LiveEventService.Application.Features.Events.Queries.ListEvents;

public class ListEventsSpecification : BaseSpecification<EventEntity>
{
    public ListEventsSpecification(
        bool? isPublished,
        string? organizerId,
        bool? isUpcoming)
    {
        Criteria = e =>
            (!isPublished.HasValue || e.IsPublished == isPublished.Value) &&
            (string.IsNullOrEmpty(organizerId) || e.OrganizerId == organizerId) &&
            (!isUpcoming.HasValue || !isUpcoming.Value || e.StartDate > DateTime.UtcNow);
        ApplyOrderBy(e => e.StartDate);
    }
}
