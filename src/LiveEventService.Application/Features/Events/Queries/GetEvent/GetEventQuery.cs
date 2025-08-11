using LiveEventService.Application.Common.Models;
using MediatR;

namespace LiveEventService.Application.Features.Events.Event.Get;

public class GetEventQuery : IRequest<BaseResponse<EventDto>>
{
    public Guid EventId { get; set; }
}
