using LiveEventService.Application.Common.Models;
using MediatR;

namespace LiveEventService.Application.Features.Events.Event.Delete;

public class DeleteEventCommand : IRequest<BaseResponse<bool>>
{
    public Guid EventId { get; set; }
    public string UserId { get; set; } = string.Empty;
}
