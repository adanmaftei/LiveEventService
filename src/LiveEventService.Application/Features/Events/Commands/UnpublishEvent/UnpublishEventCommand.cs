using LiveEventService.Application.Common.Models;
using LiveEventService.Application.Features.Events.Event;
using MediatR;

namespace LiveEventService.Application.Features.Events.Commands.UnpublishEvent;

public class UnpublishEventCommand : IRequest<BaseResponse<EventDto>>
{
    public Guid EventId { get; set; }
    public string AdminUserId { get; set; } = string.Empty;
}
