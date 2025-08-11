namespace LiveEventService.Application.Features.Events.Event;

public class EventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string TimeZone { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int AvailableSeats { get; set; }
    public bool IsPublished { get; set; }
    public string OrganizerId { get; set; } = string.Empty;
    public string OrganizerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string Address { get; set; } = string.Empty; // Optional: full address for physical events
    public string? OnlineMeetingUrl { get; set; } // Optional: URL for online events
    public int AvailableSpots => AvailableSeats; // Alias for compatibility with API
}
