using LiveEventService.Core.Common;
using EventEntity = LiveEventService.Core.Events.Event;

namespace LiveEventService.Application.Features.Events.Queries.ListEvents;

/// <summary>
/// Specification for filtering and ordering events for list queries.
/// </summary>
public class ListEventsSpecification : BaseSpecification<EventEntity>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListEventsSpecification"/> class.
    /// Creates a specification to filter by publication state, organizer, and upcoming status.
    /// </summary>
    /// <param name="isPublished">Optional flag to filter by published status.</param>
    /// <param name="organizerId">Optional organizer ID to filter by.</param>
    /// <param name="isUpcoming">Optional flag to filter for upcoming events only.</param>
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
