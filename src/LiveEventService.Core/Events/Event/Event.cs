using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.Core.Common;

namespace LiveEventService.Core.Events;

public class Event : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public int Capacity { get; private set; }
    public string TimeZone { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public string OrganizerId { get; private set; } = string.Empty;
    public bool IsPublished { get; private set; }

    public virtual ICollection<EventRegistration> Registrations { get; private set; } = new List<EventRegistration>();

    protected Event() { } // For EF Core

    public Event(string name, string description, DateTime startDate, DateTime endDate, int capacity, string timeZone, string location, string organizerId)
    {
        Name = name;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        Capacity = capacity;
        TimeZone = timeZone;
        Location = location;
        OrganizerId = organizerId;
    }

    public void UpdateDetails(string name, string description, DateTime startDate, DateTime endDate, int capacity, string timeZone, string location)
    {
        Name = name;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        Capacity = capacity;
        TimeZone = timeZone;
        Location = location;
    }

    public void AddRegistration(EventRegistration registration)
    {
        Registrations.Add(registration);
    }

    public void RemoveRegistration(EventRegistration registration)
    {
        Registrations.Remove(registration);
    }
    
    // Business logic methods (not EF properties)
    public bool IsFull()
    {
        return Registrations.Count(r => r.Status == RegistrationStatus.Confirmed) >= Capacity;
    }
    
    public void Publish() => IsPublished = true;
    public void Unpublish() => IsPublished = false;
} 