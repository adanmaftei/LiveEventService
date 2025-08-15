using LiveEventService.Application.Common.Models;
using MediatR;

namespace LiveEventService.Application.Features.Events.Event.Update;

/// <summary>
/// Command to update mutable fields of an existing event.
/// </summary>
public class UpdateEventCommand : IRequest<BaseResponse<EventDto>>
{
    /// <summary>
    /// Gets or sets identifier of the event to update.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets new values for the event; null properties are ignored.
    /// </summary>
    public UpdateEventDto Event { get; set; } = null!;

    /// <summary>
    /// Gets or sets identity ID of the organizer performing the update.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
}
