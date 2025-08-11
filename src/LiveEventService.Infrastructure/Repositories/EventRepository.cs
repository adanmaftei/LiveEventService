using LiveEventService.Core.Events;
using LiveEventService.Infrastructure.Data;
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
        return await _dbContext.EventRegistrations.CountAsync(r => r.EventId == eventId, cancellationToken);
    }
}
