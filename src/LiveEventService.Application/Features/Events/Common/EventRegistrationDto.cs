namespace LiveEventService.Application.Features.Events.EventRegistration;

/// <summary>
/// Represents a user's registration for an event.
/// </summary>
public class EventRegistrationDto
{
    /// <summary>Gets or sets registration identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets associated event identifier.</summary>
    public Guid EventId { get; set; }

    /// <summary>Gets or sets registered user's identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets registered user's display name.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Gets or sets registered user's email address.</summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>Gets or sets uTC date/time when registration occurred.</summary>
    public DateTime RegistrationDate { get; set; }

    /// <summary>Gets or sets registration status (e.g., Confirmed, Waitlisted, Cancelled).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets position in the waitlist queue if waitlisted.</summary>
    public int? PositionInQueue { get; set; }

    /// <summary>Gets or sets optional notes added by the user or admin.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets a value indicating whether true when the registration is on a waitlist.</summary>
    public bool IsWaitlisted => PositionInQueue.HasValue && PositionInQueue > 0;
}

/// <summary>
/// Request payload to create a registration.
/// </summary>
public class CreateEventRegistrationDto
{
    /// <summary>Gets or sets target event identifier.</summary>
    public Guid EventId { get; set; }

    /// <summary>Gets or sets optional notes for the organizer.</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Request payload to update a registration status.
/// </summary>
public class UpdateEventRegistrationStatusDto
{
    /// <summary>Gets or sets new status value.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets optional notes for the change.</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Paginated list of event registrations with metadata.
/// </summary>
public class EventRegistrationListDto
{
    /// <summary>Gets or sets page of registration items.</summary>
    public IEnumerable<EventRegistrationDto> Items { get; set; } = new List<EventRegistrationDto>();

    /// <summary>Gets or sets total number of registrations for the query.</summary>
    public int TotalCount { get; set; }

    /// <summary>Gets or sets current page number (1-based).</summary>
    public int PageNumber { get; set; }

    /// <summary>Gets or sets number of items per page.</summary>
    public int PageSize { get; set; }

    /// <summary>Gets total number of pages.</summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
