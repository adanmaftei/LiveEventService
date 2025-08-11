using Ardalis.GuardClauses;
using LiveEventService.Core.Common;
using LiveEventService.Core.Events;
using User = LiveEventService.Core.Users.User.User;
using MediatR;

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
        Status = RegistrationStatus.Pending;
        Notes = notes;
        
        // If event is full, add to waitlist
        if (@event.IsFull())
        {
            AddToWaitlist();
        }
        else
        {
            Status = RegistrationStatus.Confirmed;
        }
        AddDomainEvent(new EventRegistrationCreatedDomainEvent(this));
    }

    public void Confirm()
    {
        var wasWaitlisted = Status == RegistrationStatus.Waitlisted;
        if (Status == RegistrationStatus.Confirmed)
            return;
        
        if (Status != RegistrationStatus.Pending && Status != RegistrationStatus.Waitlisted)
            throw new InvalidOperationException("Only pending or waitlisted registrations can be confirmed");
        
        Status = RegistrationStatus.Confirmed;
        PositionInQueue = null;
        UpdatedAt = DateTime.UtcNow;
        if (wasWaitlisted)
        {
            AddDomainEvent(new EventRegistrationPromotedDomainEvent(this));
        }
    }

    public void Cancel()
    {
        if (Status == RegistrationStatus.Cancelled)
            return;
        
        Status = RegistrationStatus.Cancelled;
        PositionInQueue = null;
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

    private void AddToWaitlist()
    {
        Status = RegistrationStatus.Waitlisted;
        // Position will be updated by the event aggregate when processed
    }

    public void UpdateWaitlistPosition(int position)
    {
        if (Status != RegistrationStatus.Waitlisted)
            throw new InvalidOperationException("Only waitlisted registrations can have their position updated");
            
        PositionInQueue = position;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Business logic methods (not EF properties)
    public bool IsWaitlisted() => PositionInQueue.HasValue && PositionInQueue > 0;
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

public class EventRegistrationCreatedDomainEvent : DomainEvent, INotification
{
    public EventRegistration Registration { get; }
    public EventRegistrationCreatedDomainEvent(EventRegistration registration)
    {
        Registration = registration;
    }
}

public class EventRegistrationPromotedDomainEvent : DomainEvent, INotification
{
    public EventRegistration Registration { get; }
    public EventRegistrationPromotedDomainEvent(EventRegistration registration)
    {
        Registration = registration;
    }
}

public class EventRegistrationCancelledDomainEvent : DomainEvent, INotification
{
    public EventRegistration Registration { get; }
    public EventRegistrationCancelledDomainEvent(EventRegistration registration)
    {
        Registration = registration;
    }
} 