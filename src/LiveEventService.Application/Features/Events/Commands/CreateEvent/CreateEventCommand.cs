using LiveEventService.Application.Common.Models;
using MediatR;

namespace LiveEventService.Application.Features.Events.Event.Create;

public class CreateEventCommand : IRequest<BaseResponse<EventDto>>
{
    public CreateEventDto Event { get; set; } = null!;
    public string OrganizerId { get; set; } = string.Empty;
}
