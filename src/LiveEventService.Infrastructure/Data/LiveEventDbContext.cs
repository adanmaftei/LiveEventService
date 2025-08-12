using LiveEventService.Core.Events;
using LiveEventService.Core.Common;
using LiveEventService.Core.Registrations.EventRegistration;
using Microsoft.EntityFrameworkCore;
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
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();



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

        // Outbox table configuration
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("OutboxMessages");
            builder.HasKey(o => o.Id);
            builder.Property(o => o.EventType).IsRequired().HasMaxLength(512);
            builder.Property(o => o.Payload).IsRequired();
            builder.HasIndex(o => new { o.Status, o.NextAttemptAt });
        });
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

        // Collect domain events BEFORE saving, to ensure atomic outbox write with state changes
        var domainEntities = ChangeTracker.Entries<Entity>()
            .Where(x => x.Entity.DomainEvents.Any())
            .Select(x => x.Entity)
            .ToList();

        if (domainEntities.Count > 0)
        {
            // Copy events for outbox; we will persist them atomically with state changes below
            var copiedEvents = domainEntities
                .SelectMany(e => e.DomainEvents)
                .ToList();

            foreach (var domainEvent in copiedEvents)
            {
                var outbox = new OutboxMessage
                {
                    EventType = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName ?? string.Empty,
                    Payload = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Type = domainEvent.GetType().Name,
                        OccurredOn = domainEvent.DateOccurred
                    }),
                    OccurredOn = DateTime.UtcNow,
                    Status = OutboxStatus.Pending,
                    TryCount = 0,
                    NextAttemptAt = DateTime.UtcNow
                };
                await OutboxMessages.AddAsync(outbox, cancellationToken);
            }
        }

        // Persist entity changes and outbox entries in a single transaction
        var result = await base.SaveChangesAsync(cancellationToken);

        // After commit, dispatch in-process events to preserve current behavior
        if (domainEntities.Count > 0)
        {
            await _domainEventDispatcher.DispatchAndClearEventsAsync(domainEntities, cancellationToken);
        }

        return result;
    }
}
