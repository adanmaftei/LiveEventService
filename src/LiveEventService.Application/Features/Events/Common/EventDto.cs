namespace LiveEventService.Application.Features.Events.Event;

/// <summary>
/// Represents a published or draft event exposed by the API.
/// </summary>
public class EventDto
{
    /// <summary>Gets or sets event identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets title of the event.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets markdown or plain-text description of the event.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets start date/time in UTC.</summary>
    public DateTime StartDateTime { get; set; }

    /// <summary>Gets or sets end date/time in UTC.</summary>
    public DateTime EndDateTime { get; set; }

    /// <summary>Gets or sets iANA time zone of the event (e.g., "America/New_York").</summary>
    public string TimeZone { get; set; } = string.Empty;

    /// <summary>Gets or sets location name or address label.</summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>Gets or sets maximum number of registrations allowed.</summary>
    public int Capacity { get; set; }

    /// <summary>Gets or sets current number of available seats.</summary>
    public int AvailableSeats { get; set; }

    /// <summary>Gets or sets a value indicating whether whether the event is published publicly.</summary>
    public bool IsPublished { get; set; }

    /// <summary>Gets or sets organizer's identity ID.</summary>
    public string OrganizerId { get; set; } = string.Empty;

    /// <summary>Gets or sets display name of the organizer.</summary>
    public string OrganizerName { get; set; } = string.Empty;

    /// <summary>Gets or sets uTC timestamp when the event was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets uTC timestamp when the event was last updated, if ever.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Gets or sets full address for physical events.</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Gets or sets join URL for online events.</summary>
    public string? OnlineMeetingUrl { get; set; }

    /// <summary>Gets alias for <see cref="AvailableSeats"/> kept for backward compatibility.</summary>
    public int AvailableSpots => AvailableSeats;
}
