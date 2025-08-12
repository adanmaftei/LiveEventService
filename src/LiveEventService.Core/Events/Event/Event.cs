using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.Core.Common;
using System.Collections.ObjectModel;

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
    public bool IsWaitlistOpen { get; private set; } = true;
    public int AvailableSpots => Capacity - ConfirmedRegistrationsCount;
    public int ConfirmedRegistrationsCount => Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
    public int WaitlistedRegistrationsCount => Registrations.Count(r => r.Status == RegistrationStatus.Waitlisted);

    private readonly List<EventRegistration> _registrations = new();
    public virtual IReadOnlyCollection<EventRegistration> Registrations => new ReadOnlyCollection<EventRegistration>(_registrations);

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
        var oldCapacity = Capacity;
        
        Name = name;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        TimeZone = timeZone;
        Location = location;
        
        // If capacity increased, raise domain event
        if (capacity > oldCapacity)
        {
            var additionalCapacity = capacity - oldCapacity;
            Capacity = capacity;
            AddDomainEvent(new EventCapacityIncreasedDomainEvent(this, additionalCapacity));
        }
        else
        {
            Capacity = capacity;
        }
    }

    public void AddRegistration(EventRegistration registration)
    {
        _registrations.Add(registration);
    }

    public void RemoveRegistration(EventRegistration registration)
    {
        _registrations.Remove(registration);
    }
    
    // Business logic methods (not EF properties)
    public bool IsFull() => ConfirmedRegistrationsCount >= Capacity;
    
    public void Publish() => IsPublished = true;
    public void Unpublish() => IsPublished = false;
    
    public void CloseWaitlist() => IsWaitlistOpen = false;
    public void ReopenWaitlist() => IsWaitlistOpen = true;
    
    public void IncreaseCapacity(int additionalCapacity)
    {
        if (additionalCapacity <= 0)
        {
            throw new ArgumentException("Additional capacity must be positive", nameof(additionalCapacity));
        }
        
        Capacity += additionalCapacity;
        AddDomainEvent(new EventCapacityIncreasedDomainEvent(this, additionalCapacity));
    }
    
    public int GetNextWaitlistPosition() => WaitlistedRegistrationsCount + 1;
    
    public EventRegistration? GetNextWaitlistedRegistration()
        => _registrations
            .Where(r => r.Status == RegistrationStatus.Waitlisted)
            .OrderBy(r => r.PositionInQueue)
            .FirstOrDefault();
    
    public void UpdateWaitlistPositions(EventRegistration? promotedRegistration = null)
    {
        var waitlisted = _registrations
            .Where(r => r.Status == RegistrationStatus.Waitlisted)
            .OrderBy(r => r.PositionInQueue)
            .ToList();

        // If a registration was promoted, remove it from the waitlist
        if (promotedRegistration != null)
        {
            waitlisted.RemoveAll(r => r.Id == promotedRegistration.Id);
        }

        // Update positions
        for (int i = 0; i < waitlisted.Count; i++)
        {
            var registration = waitlisted[i];
            var oldPosition = registration.PositionInQueue;
            var newPosition = i + 1;
            
            if (oldPosition != newPosition)
            {
                registration.UpdateWaitlistPosition(newPosition);
            }
        }
    }
}
