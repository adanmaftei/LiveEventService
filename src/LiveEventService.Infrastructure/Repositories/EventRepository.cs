using LiveEventService.Core.Events;
using LiveEventService.Infrastructure.Data;
using LiveEventService.Core.Registrations.EventRegistration;
using Microsoft.EntityFrameworkCore;

namespace LiveEventService.Infrastructure.Events;

/// <summary>
/// Repository implementation for <see cref="Event"/> providing event-specific query methods.
/// </summary>
public class EventRepository : RepositoryBase<Event>, IEventRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context for data access.</param>
    public EventRepository(LiveEventDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// Gets upcoming published events ordered by start date.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Published events starting in the future ordered by start date.</returns>
    public async Task<IEnumerable<Event>> GetUpcomingEventsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Events
            .AsNoTracking()
            .Where(e => e.StartDate > DateTime.UtcNow && e.IsPublished)
            .OrderBy(e => e.StartDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets events organized by a specific user.
    /// </summary>
    /// <param name="organizerId">Organizer identity id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Events organized by the specified user.</returns>
    public async Task<IEnumerable<Event>> GetEventsByOrganizerAsync(string organizerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Events
            .AsNoTracking()
            .Where(e => e.OrganizerId == organizerId)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets a page of published events.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Page of published events ordered by start date descending.</returns>
    public async Task<IReadOnlyList<Event>> GetPublishedEventsAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.StartDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if a user is registered for a specific event.
    /// </summary>
    /// <param name="eventId">Event identifier.</param>
    /// <param name="userId">User identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a registration exists for the user and event.</returns>
    public Task<bool> IsUserRegisteredForEventAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.EventRegistrations.AnyAsync(r => r.EventId == eventId && r.UserId == userId, cancellationToken);
    }

    /// <summary>
    /// Gets the count of active registrations for an event.
    /// </summary>
    /// <param name="eventId">Event identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of non-cancelled registrations for the event.</returns>
    public Task<int> GetRegistrationCountForEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return _dbContext.EventRegistrations.CountAsync(r => r.EventId == eventId && r.Status != RegistrationStatus.Cancelled, cancellationToken);
    }

    /// <summary>
    /// Gets the count of waitlisted registrations for an event.
    /// </summary>
    /// <param name="eventId">Event identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of registrations currently waitlisted for the event.</returns>
    public Task<int> GetWaitlistCountForEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return _dbContext.EventRegistrations.CountAsync(r => r.EventId == eventId && r.Status == RegistrationStatus.Waitlisted, cancellationToken);
    }

    /// <summary>
    /// Calculates the position of a registration in the waitlist for an event.
    /// </summary>
    /// <param name="eventId">Event identifier.</param>
    /// <param name="registrationId">Registration identifier to evaluate position for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>1-based position of the registration in the waitlist.</returns>
    public async Task<int> CalculateWaitlistPositionAsync(Guid eventId, Guid registrationId, CancellationToken cancellationToken = default)
    {
        // Calculate position based on the order of creation (using CreatedAt and Id for tie-breaking)
        // Count how many waitlisted registrations for this event were created before this one
        var currentRegistration = await _dbContext.EventRegistrations
            .AsNoTracking()
            .Where(r => r.Id == registrationId)
            .Select(r => new { r.CreatedAt }) // Only select needed fields
            .FirstOrDefaultAsync(cancellationToken);

        if (currentRegistration == null)
        {
            throw new InvalidOperationException($"Registration {registrationId} not found");
        }

        var position = await _dbContext.EventRegistrations
            .AsNoTracking()
            .Where(r => r.EventId == eventId &&
                       r.Status == RegistrationStatus.Waitlisted &&
                       (r.CreatedAt < currentRegistration.CreatedAt ||
                        (r.CreatedAt == currentRegistration.CreatedAt && r.Id.CompareTo(registrationId) < 0)))
            .CountAsync(cancellationToken);

        return position + 1; // Position is 1-based
    }
}
