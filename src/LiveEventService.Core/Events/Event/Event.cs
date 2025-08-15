using System.Collections.ObjectModel;
using LiveEventService.Core.Common;
using LiveEventService.Core.Registrations.EventRegistration;

namespace LiveEventService.Core.Events;

/// <summary>
/// Aggregate root representing a live event that users can register for.
/// </summary>
public class Event : Entity
{
    /// <summary>Gets event name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets event description.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Gets event start date/time in UTC.</summary>
    public DateTime StartDate { get; private set; }

    /// <summary>Gets event end date/time in UTC.</summary>
    public DateTime EndDate { get; private set; }

    /// <summary>Gets maximum number of confirmed registrations allowed.</summary>
    public int Capacity { get; private set; }

    /// <summary>Gets iANA time zone identifier, e.g., "America/Los_Angeles".</summary>
    public string TimeZone { get; private set; } = string.Empty;

    /// <summary>Gets event location string.</summary>
    public string Location { get; private set; } = string.Empty;

    /// <summary>Gets organizer user identifier.</summary>
    public string OrganizerId { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether whether the event is published and visible.</summary>
    public bool IsPublished { get; private set; }

    /// <summary>Gets a value indicating whether whether waitlist is open for new waitlisted registrations.</summary>
    public bool IsWaitlistOpen { get; private set; } = true;

    /// <summary>Gets computed number of available spots based on capacity and confirmed registrations.</summary>
    public int AvailableSpots => Capacity - ConfirmedRegistrationsCount;

    /// <summary>Gets number of registrations with status Confirmed.</summary>
    public int ConfirmedRegistrationsCount => Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);

    /// <summary>Gets number of registrations with status Waitlisted.</summary>
    public int WaitlistedRegistrationsCount => Registrations.Count(r => r.Status == RegistrationStatus.Waitlisted);

    private readonly List<EventRegistration> registrations = new();

    /// <summary>Gets all registrations for this event.</summary>
    public virtual IReadOnlyCollection<EventRegistration> Registrations => new ReadOnlyCollection<EventRegistration>(registrations);

    protected Event() { } // For EF Core

    /// <summary>
    /// Initializes a new instance of the <see cref="Event"/> class.
    /// Creates a new event aggregate.
    /// </summary>
    /// <param name="name">The name of the event.</param>
    /// <param name="description">The description of the event.</param>
    /// <param name="startDate">The start date and time of the event in UTC.</param>
    /// <param name="endDate">The end date and time of the event in UTC.</param>
    /// <param name="capacity">The maximum number of confirmed registrations allowed for the event.</param>
    /// <param name="timeZone">The IANA time zone identifier for the event, e.g., "America/Los_Angeles".</param>
    /// <param name="location">The location of the event.</param>
    /// <param name="organizerId">The identifier of the user organizing the event.</param>
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

    /// <summary>
    /// Updates event details and raises an <see cref="EventCapacityIncreasedDomainEvent"/> if capacity increases.
    /// </summary>
    /// <param name="name">The name of the event.</param>
    /// <param name="description">The description of the event.</param>
    /// <param name="startDate">The start date and time of the event in UTC.</param>
    /// <param name="endDate">The end date and time of the event in UTC.</param>
    /// <param name="capacity">The maximum number of confirmed registrations allowed for the event.</param>
    /// <param name="timeZone">The IANA time zone identifier for the event, e.g., "America/Los_Angeles".</param>
    /// <param name="location">The location of the event.</param>
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

    /// <summary>
    /// Adds a registration to the event.
    /// </summary>
    /// <param name="registration">The registration to be added to the event.</param>
    public void AddRegistration(EventRegistration registration)
    {
        registrations.Add(registration);
    }

    /// <summary>
    /// Removes a registration from the event.
    /// </summary>
    /// <param name="registration">The registration to be removed from the event.</param>
    public void RemoveRegistration(EventRegistration registration)
    {
        registrations.Remove(registration);
    }

    /// <summary>
    /// Returns true if the number of confirmed registrations meets or exceeds the event's capacity.
    /// </summary>
    /// <returns>
    /// A boolean value indicating whether the event is full.
    /// </returns>
    public bool IsFull() => ConfirmedRegistrationsCount >= Capacity;

    /// <summary>Marks the event as published.</summary>
    public void Publish() => IsPublished = true;

    /// <summary>Marks the event as unpublished.</summary>
    public void Unpublish() => IsPublished = false;

    /// <summary>Closes the waitlist to new waitlisted registrations.</summary>
    public void CloseWaitlist() => IsWaitlistOpen = false;

    /// <summary>Reopens the waitlist.</summary>
    public void ReopenWaitlist() => IsWaitlistOpen = true;

    /// <summary>
    /// Increases event capacity and raises an <see cref="EventCapacityIncreasedDomainEvent"/>.
    /// </summary>
    /// <param name="additionalCapacity">The number of additional spots to be added to the event's capacity.</param>
    public void IncreaseCapacity(int additionalCapacity)
    {
        if (additionalCapacity <= 0)
        {
            throw new ArgumentException("Additional capacity must be positive", nameof(additionalCapacity));
        }

        Capacity += additionalCapacity;
        AddDomainEvent(new EventCapacityIncreasedDomainEvent(this, additionalCapacity));
    }

    /// <summary>
    /// Calculates the next waitlist position.
    /// </summary>
    /// <returns>
    /// The next position in the waitlist, which is one greater than the current count of waitlisted registrations.
    /// </returns>
    public int GetNextWaitlistPosition() => WaitlistedRegistrationsCount + 1;

    /// <summary>
    /// Gets the next registration in the waitlist ordered by position.
    /// Returns the next waitlisted registration or null if no waitlisted registrations exist.
    /// </summary>
    /// <returns>
    /// The next waitlisted registration, or null if there are no waitlisted registrations.
    /// </returns>
    public EventRegistration? GetNextWaitlistedRegistration()
        => registrations
            .Where(r => r.Status == RegistrationStatus.Waitlisted)
            .OrderBy(r => r.PositionInQueue)
            .FirstOrDefault();

    /// <summary>
    /// Recalculates waitlist positions, optionally excluding a promoted registration.
    /// </summary>
    /// <param name="promotedRegistration">
    /// The registration that has been promoted from the waitlist. If provided, this registration will be excluded from the updated waitlist positions.
    /// </param>
    public void UpdateWaitlistPositions(EventRegistration? promotedRegistration = null)
    {
        var waitlisted = registrations
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
