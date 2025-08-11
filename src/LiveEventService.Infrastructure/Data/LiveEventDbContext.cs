using LiveEventService.Core.Events;
using LiveEventService.Core.Common;
using LiveEventService.Core.Registrations.EventRegistration;
using Microsoft.EntityFrameworkCore;
using IUserRepository = LiveEventService.Core.Users.User.IUserRepository;
using UserEntity = LiveEventService.Core.Users.User.User;
using EventRegistrationEntity = LiveEventService.Core.Registrations.EventRegistration.EventRegistration;
using EventRegistrationConfiguration = LiveEventService.Infrastructure.Registrations.EventRegistrationConfiguration;
using UserConfiguration = LiveEventService.Infrastructure.Users.UserConfiguration;
using EventConfiguration = LiveEventService.Infrastructure.Events.EventConfiguration;

namespace LiveEventService.Infrastructure.Data;

public class LiveEventDbContext : DbContext
{
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    public LiveEventDbContext(DbContextOptions<LiveEventDbContext> options, IDomainEventDispatcher domainEventDispatcher)
        : base(options)
    {
        _domainEventDispatcher = domainEventDispatcher;
    }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<EventRegistrationEntity> EventRegistrations => Set<EventRegistrationEntity>();



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore abstract domain event class
        modelBuilder.Ignore<DomainEvent>();

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new EventConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new EventRegistrationConfiguration());

        // Configure global query filters
        // modelBuilder.Entity<Event>().HasQueryFilter(e => !e.IsDeleted);
            
        modelBuilder.Entity<UserEntity>()
            .HasQueryFilter(u => u.IsActive);
            
        modelBuilder.Entity<EventRegistrationEntity>()
            .HasQueryFilter(er => er.Status != RegistrationStatus.Cancelled);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is Entity && (
                e.State == EntityState.Added
                || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            var entity = (Entity)entityEntry.Entity;
            if (entityEntry.State == EntityState.Added)
            {
                typeof(Entity).GetProperty("CreatedAt")?.SetValue(entity, DateTime.UtcNow);
            }
            typeof(Entity).GetProperty("UpdatedAt")?.SetValue(entity, DateTime.UtcNow);
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        // Dispatch domain events
        var domainEntities = ChangeTracker.Entries<Entity>()
            .Where(x => x.Entity.DomainEvents.Any())
            .Select(x => x.Entity)
            .ToList();
        if (domainEntities.Any())
        {
            await _domainEventDispatcher.DispatchAndClearEventsAsync(domainEntities, cancellationToken);
        }

        return result;
    }
}
