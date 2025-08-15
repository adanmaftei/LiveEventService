using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Features.Events.Event;
using MediatR;

namespace LiveEventService.Application.Features.Events.Commands.UnpublishEvent;

/// <summary>
/// Command to unpublish an event, hiding it from registration.
/// </summary>
public class UnpublishEventCommand : IRequest<BaseResponse<EventDto>>
{
    /// <summary>
    /// Gets or sets identifier of the event to unpublish.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets identity ID of the admin initiating unpublication.
    /// </summary>
    public string AdminUserId { get; set; } = string.Empty;
}
