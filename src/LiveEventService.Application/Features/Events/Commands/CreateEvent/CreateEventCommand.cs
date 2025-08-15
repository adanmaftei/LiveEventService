using LiveEventService.Application.Common.Models;
using MediatR;

namespace LiveEventService.Application.Features.Events.Event.Create;

/// <summary>
/// Command to create a new event for a given organizer.
/// </summary>
public class CreateEventCommand : IRequest<BaseResponse<EventDto>>
{
    /// <summary>
    /// Gets or sets details of the event to create.
    /// </summary>
    public CreateEventDto Event { get; set; } = null!;

    /// <summary>
    /// Gets or sets identity ID of the organizer creating the event.
    /// </summary>
    public string OrganizerId { get; set; } = string.Empty;
}
