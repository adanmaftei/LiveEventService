using LiveEventService.Application.Common.Models;
using MediatR;

namespace LiveEventService.Application.Features.Events.Event.Delete;

/// <summary>
/// Command to delete an existing event by its identifier.
/// </summary>
public class DeleteEventCommand : IRequest<BaseResponse<bool>>
{
    /// <summary>
    /// Gets or sets identifier of the event to delete.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets identity ID of the requesting user (must be the organizer).
    /// </summary>
    public string UserId { get; set; } = string.Empty;
}
