namespace LiveEventService.Application.Features.Events.Event;

/// <summary>
/// Request payload to create a new event.
/// </summary>
public class CreateEventDto
{
    /// <summary>Gets or sets title of the event.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets markdown or plain-text description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets start date/time in UTC.</summary>
    public DateTime StartDateTime { get; set; }

    /// <summary>Gets or sets end date/time in UTC.</summary>
    public DateTime EndDateTime { get; set; }

    /// <summary>Gets or sets iANA time zone (e.g., "America/New_York").</summary>
    public string TimeZone { get; set; } = string.Empty;

    /// <summary>Gets or sets location or venue.</summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>Gets or sets maximum allowed registrations.</summary>
    public int Capacity { get; set; }
}
