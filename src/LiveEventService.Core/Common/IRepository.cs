namespace LiveEventService.Core.Common;

/// <summary>
/// Generic repository abstraction for aggregate roots and entities.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IRepository<T>
    where T : Entity
{
    // Write operations

    /// <summary>
    /// Adds a new entity instance.
    /// </summary>
    /// <param name="entity">The entity to be added.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The added entity instance.</returns>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity instance.
    /// </summary>
    /// <param name="entity">The entity to be updated.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity instance.
    /// </summary>
    /// <param name="entity">The entity to be deleted.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

    // Read operations (with change tracking)

    /// <summary>
    /// Gets an entity by identifier with change tracking enabled.
    /// </summary>
    /// <param name="id">The identifier of the entity.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The entity instance, or null if not found.</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all entities with change tracking enabled.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only list of all entities.</returns>
    Task<IReadOnlyList<T>> ListAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists entities matching a specification with tracking enabled.
    /// </summary>
    /// <param name="spec">The specification to filter entities.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only list of matching entities.</returns>
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity that matches a specification, or null.
    /// </summary>
    /// <param name="spec">The specification to filter entities.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The first matching entity, or null if none found.</returns>
    Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);

    // Read operations (without change tracking - optimized for read-only scenarios)

    /// <summary>
    /// Gets an entity by identifier without change tracking.
    /// </summary>
    /// <param name="id">The identifier of the entity.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The entity instance, or null if not found.</returns>
    Task<T?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all entities without change tracking.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only list of all entities.</returns>
    Task<IReadOnlyList<T>> ListAllReadOnlyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists entities matching a specification without tracking.
    /// </summary>
    /// <param name="spec">The specification to filter entities.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only list of matching entities.</returns>
    Task<IReadOnlyList<T>> ListReadOnlyAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity that matches a specification without tracking, or null.
    /// </summary>
    /// <param name="spec">The specification to filter entities.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The first matching entity, or null if none found.</returns>
    Task<T?> FirstOrDefaultReadOnlyAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);

    // Query operations

    /// <summary>
    /// Counts entities that match a specification.
    /// </summary>
    /// <param name="spec">The specification to filter entities.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The count of matching entities.</returns>
    Task<int> CountAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether any entity matches a specification.
    /// </summary>
    /// <param name="spec">The specification to filter entities.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>True if any entity matches the specification; otherwise, false.</returns>
    Task<bool> AnyAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
}
