using LiveEventService.Core.Common;
using Microsoft.EntityFrameworkCore;

namespace LiveEventService.Infrastructure.Data;

/// <summary>
/// Generic EF Core repository implementing the <see cref="IRepository{T}"/> abstraction
/// with support for specification queries and optional no-tracking read paths.
/// </summary>
/// <typeparam name="T">The type of entity this repository manages.</typeparam>
public class RepositoryBase<T> : IRepository<T> where T : Entity
{
    // These fields are intentionally protected for use by derived repositories.
    // They are marked as protected to allow derived repositories to access them
    // while maintaining encapsulation from external consumers.
#pragma warning disable SA1401 // Fields are intentionally protected for inheritance
    protected readonly LiveEventDbContext _dbContext;

    protected readonly DbSet<T> _dbSet;
#pragma warning restore SA1401

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryBase{T}"/> class.
    /// Creates a new repository instance bound to the provided DbContext.
    /// </summary>
    /// <param name="dbContext">The database context for data access.</param>
    public RepositoryBase(LiveEventDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dbSet = _dbContext.Set<T>();
    }

    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <summary>
    /// Gets an entity by ID without change tracking for read-only scenarios.
    /// </summary>
    /// <param name="id">Entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity if found; otherwise null.</returns>
    public virtual Task<T?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<T>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Lists all entities without change tracking for read-only scenarios.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All entities as a read-only list.</returns>
    public virtual async Task<IReadOnlyList<T>> ListAllReadOnlyAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Lists entities using specification without change tracking for read-only scenarios.
    /// </summary>
    /// <param name="spec">Specification predicate and includes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching entities as a read-only list.</returns>
    public virtual async Task<IReadOnlyList<T>> ListReadOnlyAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec, useTracking: false).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <inheritdoc />
    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Entry(entity).State = EntityState.Modified;
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task<int> CountAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        return ApplySpecification(spec).CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task<bool> AnyAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        return ApplySpecification(spec).AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        return ApplySpecification(spec).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Gets first entity matching specification without change tracking for read-only scenarios.
    /// </summary>
    /// <param name="spec">Specification predicate and includes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The first matching entity, or null.</returns>
    public virtual Task<T?> FirstOrDefaultReadOnlyAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        return ApplySpecification(spec, useTracking: false).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Applies the specification to the base query, optionally disabling tracking for read-only scenarios.
    /// </summary>
    /// <param name="spec">Specification predicate and includes.</param>
    /// <param name="useTracking">When false, uses AsNoTracking for read-only queries.</param>
    /// <returns>The composed query.</returns>
    protected IQueryable<T> ApplySpecification(ISpecification<T> spec, bool useTracking = true)
    {
        var query = useTracking ? _dbSet.AsQueryable() : _dbSet.AsNoTracking();
        return SpecificationEvaluator<T>.GetQuery(query, spec);
    }
}
