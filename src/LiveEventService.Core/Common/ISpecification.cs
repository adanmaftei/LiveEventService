using System.Linq.Expressions;

namespace LiveEventService.Core.Common;

/// <summary>
/// Contract for query specifications used to describe query requirements.
/// </summary>
/// <typeparam name="T">The entity type being queried.</typeparam>
public interface ISpecification<T>
{
    /// <summary>Gets filter criteria.</summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>Gets navigation property includes (typed).</summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>Gets navigation property includes (string-based for dynamic includes).</summary>
    List<string> IncludeStrings { get; }

    /// <summary>Gets ascending ordering expression.</summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>Gets descending ordering expression.</summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>Gets grouping expression.</summary>
    Expression<Func<T, object>>? GroupBy { get; }

    /// <summary>Gets number of items to take for paging.</summary>
    int Take { get; }

    /// <summary>Gets number of items to skip for paging.</summary>
    int Skip { get; }

    /// <summary>Gets a value indicating whether whether paging is enabled.</summary>
    bool IsPagingEnabled { get; }
}
