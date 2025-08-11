using LiveEventService.Application.Common.Models;
using MediatR;

namespace LiveEventService.Application.Features.Events.EventRegistration.Get;

public class GetEventRegistrationsQuery : IRequest<BaseResponse<EventRegistrationListDto>>
{
    public Guid EventId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Status { get; set; }
    public Guid? UserId { get; set; }
}
