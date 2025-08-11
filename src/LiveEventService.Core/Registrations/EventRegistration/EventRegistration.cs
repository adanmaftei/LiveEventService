using Ardalis.GuardClauses;
using LiveEventService.Core.Common;
using LiveEventService.Core.Events;
using User = LiveEventService.Core.Users.User.User;

namespace LiveEventService.Core.Registrations.EventRegistration;

public class EventRegistration : Entity
{
    public Guid EventId { get; private set; }
    public virtual Event Event { get; private set; } = null!;
    
    public Guid UserId { get; private set; }
    public virtual User User { get; private set; } = null!;
    
    public DateTime RegistrationDate { get; private set; }
    public RegistrationStatus Status { get; private set; }
    public string? Notes { get; private set; }
    
    // For waitlist functionality
    public int? PositionInQueue { get; private set; }

    protected EventRegistration() { } // For EF Core

    public EventRegistration(Event @event, User user, string? notes = null)
    {
        Event = Guard.Against.Null(@event, nameof(@event));
        EventId = @event.Id;
        
        User = Guard.Against.Null(user, nameof(user));
        UserId = user.Id;
        
        RegistrationDate = DateTime.UtcNow;
        Notes = notes;
        
        // Determine initial status based on event capacity
        if (@event.IsFull())
        {
            Status = RegistrationStatus.Waitlisted;
            PositionInQueue = null; // Position will be set later by the service layer
        }
        else
        {
            Status = RegistrationStatus.Confirmed;
            PositionInQueue = null;
        }
        
        AddDomainEvent(new EventRegistrationCreatedDomainEvent(this));
    }

    public void Confirm()
    {
        if (Status == RegistrationStatus.Confirmed)
            return; // Already confirmed, do nothing
            
        if (Status != RegistrationStatus.Pending && Status != RegistrationStatus.Waitlisted)
            throw new InvalidOperationException("Only pending or waitlisted registrations can be confirmed");
            
        Status = RegistrationStatus.Confirmed;
        PositionInQueue = null; // No longer in queue
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new EventRegistrationPromotedDomainEvent(this));
    }

    public void Cancel()
    {
        if (Status == RegistrationStatus.Cancelled)
            return; // Already cancelled, do nothing
            
        Status = RegistrationStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new EventRegistrationCancelledDomainEvent(this));
    }

    public void MarkAsAttended()
    {
        if (Status != RegistrationStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed registrations can be marked as attended");
            
        Status = RegistrationStatus.Attended;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsNoShow()
    {
        if (Status != RegistrationStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed registrations can be marked as no-show");
            
        Status = RegistrationStatus.NoShow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddToWaitlist()
    {
        Status = RegistrationStatus.Waitlisted;
        PositionInQueue = null; // Position will be set later by the service layer
    }

    public void UpdateWaitlistPosition(int position)
    {
        if (Status != RegistrationStatus.Waitlisted)
            throw new InvalidOperationException("Only waitlisted registrations can have their position updated");
            
        PositionInQueue = position;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Business logic methods (not EF properties)
    public bool IsWaitlisted() => Status == RegistrationStatus.Waitlisted && PositionInQueue.HasValue && PositionInQueue > 0;
}

public enum RegistrationStatus
{
    Pending,    // Initial state
    Confirmed,  // Registration is confirmed
    Waitlisted, // On waitlist due to event being full
    Cancelled,  // Registration was cancelled
    Attended,   // User attended the event
    NoShow      // User didn't attend the event
} 

public class EventRegistrationCreatedDomainEvent : DomainEvent
{
    public EventRegistration Registration { get; }
    public EventRegistrationCreatedDomainEvent(EventRegistration registration)
    {
        Registration = registration;
    }
}

public class EventRegistrationPromotedDomainEvent : DomainEvent
{
    public EventRegistration Registration { get; }
    public EventRegistrationPromotedDomainEvent(EventRegistration registration)
    {
        Registration = registration;
    }
}

public class EventRegistrationCancelledDomainEvent : DomainEvent
{
    public EventRegistration Registration { get; }
    public EventRegistrationCancelledDomainEvent(EventRegistration registration)
    {
        Registration = registration;
    }
} 