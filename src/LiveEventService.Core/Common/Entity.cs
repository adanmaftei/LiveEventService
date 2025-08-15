namespace LiveEventService.Core.Common;

/// <summary>
/// Base class for entities with identity, timestamps, and domain event support.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Gets or sets unique identifier for the entity.
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets or sets uTC timestamp when the entity was created.
    /// </summary>
    public DateTime CreatedAt { get; protected set; }

    /// <summary>
    /// Gets or sets uTC timestamp when the entity was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; protected set; }

    private List<DomainEvent> domainEvents = new();

    /// <summary>
    /// Gets collection of domain events raised by this entity.
    /// </summary>
    public IReadOnlyCollection<DomainEvent> DomainEvents => domainEvents.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity"/> class.
    /// Initializes a new entity with a new identifier and creation timestamp.
    /// </summary>
    protected Entity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a domain event to the entity's queue.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events associated with the entity.
    /// </summary>
    public void ClearDomainEvents()
    {
        domainEvents.Clear();
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        if (Id == Guid.Empty || other.Id == Guid.Empty)
        {
            return false;
        }

        return Id == other.Id;
    }

    /// <summary>
    /// Compares two entities for equality.
    /// </summary>
    /// <param name="a">The first entity to compare.</param>
    /// <param name="b">The second entity to compare.</param>
    /// <returns>True if the entities are equal; otherwise false.</returns>
    public static bool operator ==(Entity? a, Entity? b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Equals(b);
    }

    /// <summary>
    /// Compares two entities for inequality.
    /// </summary>
    /// <param name="a">The first entity to compare.</param>
    /// <param name="b">The second entity to compare.</param>
    /// <returns>True if the entities are not equal; otherwise false.</returns>
    public static bool operator !=(Entity? a, Entity? b) => !(a == b);

    /// <inheritdoc />
    public override int GetHashCode() => (GetType().ToString() + Id).GetHashCode();
}
