namespace LiveEventService.Application.Features.Events.Event;

/// <summary>
/// Request payload to update an existing event.
/// </summary>
public class UpdateEventDto : CreateEventDto
{
    /// <summary>Gets or sets event identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets optional publication state. If set, toggles visibility.</summary>
    public bool? IsPublished { get; set; }
}
