using LiveEventService.Application.Common.Models;
using MediatR;

namespace LiveEventService.Application.Features.Events.Event.Get;

/// <summary>
/// Query to retrieve a single event by its identifier.
/// </summary>
public class GetEventQuery : IRequest<BaseResponse<EventDto>>
{
    /// <summary>
    /// Gets or sets identifier of the event to fetch.
    /// </summary>
    public Guid EventId { get; set; }
}
