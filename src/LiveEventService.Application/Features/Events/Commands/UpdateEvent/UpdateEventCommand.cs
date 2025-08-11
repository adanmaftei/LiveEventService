using LiveEventService.Application.Common.Models;
using MediatR;

namespace LiveEventService.Application.Features.Events.Event.Update;

public class UpdateEventCommand : IRequest<BaseResponse<EventDto>>
{
    public Guid EventId { get; set; }
    public UpdateEventDto Event { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
}
