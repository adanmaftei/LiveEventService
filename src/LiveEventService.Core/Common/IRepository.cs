namespace LiveEventService.Core.Common;

public interface IRepository<T>
    where T : Entity
{
    // Write operations
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

    // Read operations (with change tracking)
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);

    // Read operations (without change tracking - optimized for read-only scenarios)
    Task<T?> GetByIdReadOnlyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAllReadOnlyAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListReadOnlyAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultReadOnlyAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);

    // Query operations
    Task<int> CountAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
}
