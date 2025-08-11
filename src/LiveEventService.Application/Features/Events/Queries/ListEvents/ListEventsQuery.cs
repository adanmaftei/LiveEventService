using LiveEventService.Application.Common.Models;
using MediatR;

namespace LiveEventService.Application.Features.Events.Event.List;

public class ListEventsQuery : IRequest<BaseResponse<EventListDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public bool? IsPublished { get; set; }
    public string? OrganizerId { get; set; }
    public bool? IsUpcoming { get; set; }
}
