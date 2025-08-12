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
        {
            return; // Already confirmed, do nothing
        }
            
        if (Status != RegistrationStatus.Pending && Status != RegistrationStatus.Waitlisted)
        {
            throw new InvalidOperationException("Only pending or waitlisted registrations can be confirmed");
        }
            
        Status = RegistrationStatus.Confirmed;
        PositionInQueue = null; // No longer in queue
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new EventRegistrationPromotedDomainEvent(this));
    }

    public void Cancel()
    {
        if (Status == RegistrationStatus.Cancelled)
        {
            return; // Already cancelled, do nothing
        }
        
        var wasWaitlisted = Status == RegistrationStatus.Waitlisted;
        var oldPosition = PositionInQueue;
            
        Status = RegistrationStatus.Cancelled;
        PositionInQueue = null;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new EventRegistrationCancelledDomainEvent(this));
        
        // If this was a waitlisted registration, also raise a waitlist removal event
        if (wasWaitlisted)
        {
            AddDomainEvent(new WaitlistRemovalDomainEvent(this, "Registration cancelled"));
        }
    }

    public void MarkAsAttended()
    {
        if (Status != RegistrationStatus.Confirmed)
        {
            throw new InvalidOperationException("Only confirmed registrations can be marked as attended");
        }
            
        Status = RegistrationStatus.Attended;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsNoShow()
    {
        if (Status != RegistrationStatus.Confirmed)
        {
            throw new InvalidOperationException("Only confirmed registrations can be marked as no-show");
        }
            
        Status = RegistrationStatus.NoShow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddToWaitlist(int? position = null)
    {
        if (Status == RegistrationStatus.Waitlisted)
        {
            return; // Already on waitlist
        }
            
        Status = RegistrationStatus.Waitlisted;
        PositionInQueue = position; // Position can be set directly or calculated later
        
        AddDomainEvent(new RegistrationWaitlistedDomainEvent(this));
    }
    
    public void UpdateWaitlistPosition(int? position)
    {
        if (Status != RegistrationStatus.Waitlisted)
        {
            throw new InvalidOperationException("Cannot set position for non-waitlisted registration");
        }
            
        if (position.HasValue && position <= 0)
        {
            throw new ArgumentException("Position must be positive", nameof(position));
        }
            
        var oldPosition = PositionInQueue;
        PositionInQueue = position;
        
        if (oldPosition != position)
        {
            AddDomainEvent(new WaitlistPositionChangedDomainEvent(
                EventId, 
                Id, 
                oldPosition, 
                position));
        }
    }
    
    public void RemoveFromWaitlist(string reason = null)
    {
        if (Status != RegistrationStatus.Waitlisted)
        {
            throw new InvalidOperationException("Registration is not on the waitlist");
        }
            
        var oldPosition = PositionInQueue;
        Status = RegistrationStatus.Cancelled;
        PositionInQueue = null;
        
        AddDomainEvent(new WaitlistRemovalDomainEvent(this, reason));
    }
    
    // Business logic methods (not EF properties)
    public bool IsWaitlisted() => Status == RegistrationStatus.Waitlisted && PositionInQueue.HasValue && PositionInQueue > 0;
}
