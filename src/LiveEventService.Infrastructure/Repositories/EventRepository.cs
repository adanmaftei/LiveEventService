using LiveEventService.Core.Events;
using LiveEventService.Infrastructure.Data;
using LiveEventService.Core.Registrations.EventRegistration;
using Microsoft.EntityFrameworkCore;

namespace LiveEventService.Infrastructure.Events;

public class EventRepository : RepositoryBase<Event>, IEventRepository
{
    public EventRepository(LiveEventDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<Event>> GetUpcomingEventsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Events
            .Where(e => e.StartDate > DateTime.UtcNow && e.IsPublished)
            .OrderBy(e => e.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Event>> GetEventsByOrganizerAsync(string organizerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Events
            .Where(e => e.OrganizerId == organizerId)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetPublishedEventsAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.StartDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsUserRegisteredForEventAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EventRegistrations.AnyAsync(r => r.EventId == eventId && r.UserId == userId, cancellationToken);
    }

    public async Task<int> GetRegistrationCountForEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EventRegistrations.CountAsync(r => r.EventId == eventId && r.Status != RegistrationStatus.Cancelled, cancellationToken);
    }

    public async Task<int> GetWaitlistCountForEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EventRegistrations.CountAsync(r => r.EventId == eventId && r.Status == RegistrationStatus.Waitlisted, cancellationToken);
    }

    public async Task<int> CalculateWaitlistPositionAsync(Guid eventId, Guid registrationId, CancellationToken cancellationToken = default)
    {
        // Calculate position based on the order of creation (using CreatedAt and Id for tie-breaking)
        // Count how many waitlisted registrations for this event were created before this one
        var currentRegistration = await _dbContext.EventRegistrations
            .Where(r => r.Id == registrationId)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (currentRegistration == null)
        {
            throw new InvalidOperationException($"Registration {registrationId} not found");
        }

        var position = await _dbContext.EventRegistrations
            .Where(r => r.EventId == eventId && 
                       r.Status == RegistrationStatus.Waitlisted &&
                       (r.CreatedAt < currentRegistration.CreatedAt || 
                        (r.CreatedAt == currentRegistration.CreatedAt && r.Id.CompareTo(registrationId) < 0)))
            .CountAsync(cancellationToken);
        
        return position + 1; // Position is 1-based
    }
}
