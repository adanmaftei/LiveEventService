using LiveEventService.Core.Common;
using Microsoft.EntityFrameworkCore;

namespace LiveEventService.Infrastructure.Data;

public class RepositoryBase<T> : IRepository<T> where T : Entity
{
    protected readonly LiveEventDbContext _dbContext;
    protected readonly DbSet<T> _dbSet;

    public RepositoryBase(LiveEventDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dbSet = _dbContext.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <summary>
    /// Gets an entity by ID without change tracking for read-only scenarios
    /// </summary>
    public virtual async Task<T?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Lists all entities without change tracking for read-only scenarios
    /// </summary>
    public virtual async Task<IReadOnlyList<T>> ListAllReadOnlyAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Lists entities using specification without change tracking for read-only scenarios
    /// </summary>
    public virtual async Task<IReadOnlyList<T>> ListReadOnlyAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec, useTracking: false).ToListAsync(cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Entry(entity).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task<int> CountAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec).CountAsync(cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec).AnyAsync(cancellationToken);
    }

    public virtual async Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Gets first entity matching specification without change tracking for read-only scenarios
    /// </summary>
    public virtual async Task<T?> FirstOrDefaultReadOnlyAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec, useTracking: false).FirstOrDefaultAsync(cancellationToken);
    }

    protected IQueryable<T> ApplySpecification(ISpecification<T> spec, bool useTracking = true)
    {
        var query = useTracking ? _dbSet.AsQueryable() : _dbSet.AsNoTracking();
        return SpecificationEvaluator<T>.GetQuery(query, spec);
    }
}
