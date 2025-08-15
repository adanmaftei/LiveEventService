using Ardalis.GuardClauses;
using LiveEventService.Core.Common;
using LiveEventService.Core.Events;
using User = LiveEventService.Core.Users.User.User;

namespace LiveEventService.Core.Registrations.EventRegistration;

/// <summary>
/// Aggregate representing a user's registration for an event including waitlist state.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="EventRegistration"/> class.
    /// Creates a new registration for an event and raises a created domain event.
    /// </summary>
    /// <param name="event">The event for which the registration is being created.</param>
    /// <param name="user">The user registering for the event.</param>
    /// <param name="notes">Optional notes for the registration.</param>
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

    /// <summary>
    /// Confirms the registration if currently pending or waitlisted, and raises a promoted event.
    /// </summary>
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

    /// <summary>
    /// Cancels the registration and raises cancellation (and possibly waitlist removal) events.
    /// </summary>
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

    /// <summary>
    /// Marks the registration as attended. Only valid for confirmed registrations.
    /// </summary>
    public void MarkAsAttended()
    {
        if (Status != RegistrationStatus.Confirmed)
        {
            throw new InvalidOperationException("Only confirmed registrations can be marked as attended");
        }

        Status = RegistrationStatus.Attended;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the registration as no-show. Only valid for confirmed registrations.
    /// </summary>
    public void MarkAsNoShow()
    {
        if (Status != RegistrationStatus.Confirmed)
        {
            throw new InvalidOperationException("Only confirmed registrations can be marked as no-show");
        }

        Status = RegistrationStatus.NoShow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds the registration to the waitlist with an optional initial position and raises a waitlisted event.
    /// </summary>
    /// <param name="position">Optional initial position in the waitlist queue.</param>
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

    /// <summary>
    /// Updates the waitlist position and raises a position changed event if changed.
    /// </summary>
    /// <param name="position">The new position in the waitlist queue.</param>
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

    /// <summary>
    /// Removes the registration from the waitlist and sets status to Cancelled, raising a removal event.
    /// </summary>
    /// <param name="reason">Optional reason for removing from waitlist.</param>
    public void RemoveFromWaitlist(string? reason = null)
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

    /// <summary>True if the registration is waitlisted and has a valid queue position.</summary>
    /// <returns>True if the registration is waitlisted with a valid position; otherwise false.</returns>
    public bool IsWaitlisted() => Status == RegistrationStatus.Waitlisted && PositionInQueue.HasValue && PositionInQueue > 0;
}
