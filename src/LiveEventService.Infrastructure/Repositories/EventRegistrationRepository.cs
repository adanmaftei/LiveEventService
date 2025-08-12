using LiveEventService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using EventRegistrationEntity = LiveEventService.Core.Registrations.EventRegistration.EventRegistration;

namespace LiveEventService.Infrastructure.Registrations;

public class EventRegistrationRepository : RepositoryBase<EventRegistrationEntity>
{
    public EventRegistrationRepository(LiveEventDbContext dbContext) : base(dbContext)
    {
    }

    public override async Task<EventRegistrationEntity> AddAsync(EventRegistrationEntity entity, CancellationToken cancellationToken = default)
    {
        // Ensure existing navigations are not re-inserted by EF Core
        if (entity.Event != null)
        {
            _dbContext.Entry(entity.Event).State = EntityState.Unchanged;
        }
        if (entity.User != null)
        {
            _dbContext.Entry(entity.User).State = EntityState.Unchanged;
        }

        // If waitlisted and no position is set, assign position atomically to avoid race conditions
        var registrationStatusProperty = typeof(EventRegistrationEntity).GetProperty("Status");
        var positionProperty = typeof(EventRegistrationEntity).GetProperty("PositionInQueue");
        var eventIdProperty = typeof(EventRegistrationEntity).GetProperty("EventId");

        if (registrationStatusProperty != null && positionProperty != null && eventIdProperty != null)
        {
            var statusValue = registrationStatusProperty.GetValue(entity);
            var isWaitlisted = statusValue?.ToString() == "Waitlisted";
            var currentPosition = positionProperty.GetValue(entity) as int?;

            if (isWaitlisted && currentPosition == null)
            {
                // Begin transaction to acquire advisory lock and assign next position
                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                // Derive a stable 64-bit advisory lock key from EventId
                var eventId = (Guid)(eventIdProperty.GetValue(entity) ?? Guid.Empty);
                long lockKey = BitConverter.ToInt64(eventId.ToByteArray(), 0);

                // Acquire transaction-scoped advisory lock for this event
                await _dbContext.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({lockKey});", cancellationToken);

                // Compute next position under the lock
                var nextPosition = await _dbContext.EventRegistrations
                    .AsNoTracking()
                    .Where(r => r.EventId == eventId && r.Status == Core.Registrations.EventRegistration.RegistrationStatus.Waitlisted && r.PositionInQueue != null)
                    .MaxAsync(r => (int?)r.PositionInQueue!, cancellationToken) ?? 0;

                positionProperty.SetValue(entity, nextPosition + 1);

                _dbSet.Add(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return entity;
            }
        }

        return await base.AddAsync(entity, cancellationToken);
    }

    public override async Task UpdateAsync(EventRegistrationEntity entity, CancellationToken cancellationToken = default)
    {
        // Protect against accidental updates to navigations during registration updates
        if (entity.Event != null)
        {
            _dbContext.Entry(entity.Event).State = EntityState.Unchanged;
        }
        if (entity.User != null)
        {
            _dbContext.Entry(entity.User).State = EntityState.Unchanged;
        }

        await base.UpdateAsync(entity, cancellationToken);
    }
}

