using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Features.Events.Event;
using MediatR;

namespace LiveEventService.Application.Features.Events.Commands.PublishEvent;

/// <summary>
/// Command to publish an event, making it visible for registration.
/// </summary>
public class PublishEventCommand : IRequest<BaseResponse<EventDto>>
{
    /// <summary>
    /// Gets or sets identifier of the event to publish.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets identity ID of the admin initiating publication.
    /// </summary>
    public string AdminUserId { get; set; } = string.Empty;
}
