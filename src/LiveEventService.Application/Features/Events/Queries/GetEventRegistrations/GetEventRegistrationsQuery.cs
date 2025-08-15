using LiveEventService.Application.Common.Models;
using MediatR;

namespace LiveEventService.Application.Features.Events.EventRegistration.Get;

/// <summary>
/// Query to list registrations for an event with optional filtering and pagination.
/// </summary>
public class GetEventRegistrationsQuery : IRequest<BaseResponse<EventRegistrationListDto>>
{
    /// <summary>
    /// Gets or sets identifier of the event whose registrations are requested.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets 1-based page number.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets page size.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets optional registration status filter (e.g., Confirmed, Waitlisted).
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets optional filter to include registrations for a specific user.
    /// </summary>
    public Guid? UserId { get; set; }
}
