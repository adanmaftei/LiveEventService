namespace LiveEventService.Application.Features.Events.Event;

public class UpdateEventDto : CreateEventDto
{
    public Guid Id { get; set; }
    public bool? IsPublished { get; set; }
}
