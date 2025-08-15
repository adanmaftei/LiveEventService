using LiveEventService.Application.Common.Models;
using MediatR;

namespace LiveEventService.Application.Features.Events.Event.List;

/// <summary>
/// Query to list events with optional filters and pagination.
/// </summary>
public class ListEventsQuery : IRequest<BaseResponse<EventListDto>>
{
    /// <summary>
    /// Gets or sets 1-based page number.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets page size.
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets filter by publication state.
    /// </summary>
    public bool? IsPublished { get; set; }

    /// <summary>
    /// Gets or sets filter by organizer identity ID.
    /// </summary>
    public string? OrganizerId { get; set; }

    /// <summary>
    /// Gets or sets if true, only include events that have not started yet.
    /// </summary>
    public bool? IsUpcoming { get; set; }
}
