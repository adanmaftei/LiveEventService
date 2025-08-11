namespace LiveEventService.Application.Features.Events.Event;

public class EventListDto
{
    public IEnumerable<EventDto> Items { get; set; } = new List<EventDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
