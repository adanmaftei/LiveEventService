namespace LiveEventService.Application.Features.Events.Event;

/// <summary>
/// Paginated list of events with basic paging metadata.
/// </summary>
public class EventListDto
{
    /// <summary>Gets or sets page of event items.</summary>
    public IEnumerable<EventDto> Items { get; set; } = new List<EventDto>();

    /// <summary>Gets or sets total number of events matching the query.</summary>
    public int TotalCount { get; set; }

    /// <summary>Gets or sets current page number (1-based).</summary>
    public int PageNumber { get; set; }

    /// <summary>Gets or sets number of items per page.</summary>
    public int PageSize { get; set; }

    /// <summary>Gets total number of pages.</summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
