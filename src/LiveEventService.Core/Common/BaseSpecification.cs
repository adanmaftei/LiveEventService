using System.Linq.Expressions;

namespace LiveEventService.Core.Common;

/// <summary>
/// Base implementation of <see cref="ISpecification{T}"/> to facilitate building query specifications.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public abstract class BaseSpecification<T> : ISpecification<T>
    where T : Entity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSpecification{T}"/> class.
    /// Initializes a new empty specification.
    /// </summary>
    protected BaseSpecification() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSpecification{T}"/> class.
    /// Initializes a new specification with the given filter criteria.
    /// </summary>
    /// <param name="criteria">The filter criteria expression.</param>
    protected BaseSpecification(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    /// <inheritdoc />
    public Expression<Func<T, bool>>? Criteria { get; protected set; }

    /// <inheritdoc />
    public List<Expression<Func<T, object>>> Includes { get; } = new();

    /// <inheritdoc />
    public List<string> IncludeStrings { get; } = new();

    /// <inheritdoc />
    public Expression<Func<T, object>>? OrderBy { get; private set; }

    /// <inheritdoc />
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    /// <inheritdoc />
    public Expression<Func<T, object>>? GroupBy { get; private set; }

    /// <inheritdoc />
    public int Take { get; private set; }

    /// <inheritdoc />
    public int Skip { get; private set; }

    /// <inheritdoc />
    public bool IsPagingEnabled { get; private set; }

    /// <summary>
    /// Adds a strongly-typed include expression for eager-loading navigation properties.
    /// </summary>
    /// <param name="includeExpression">The expression specifying the navigation property to include.</param>
    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    /// <summary>
    /// Adds a string-based include for dynamic include paths.
    /// </summary>
    /// <param name="includeString">The string representing the navigation property path to include.</param>
    protected void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    /// <summary>
    /// Applies paging parameters to the specification.
    /// </summary>
    /// <param name="skip">The number of items to skip.</param>
    /// <param name="take">The number of items to take.</param>
    public void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    /// <summary>
    /// Applies ascending ordering to the query results.
    /// </summary>
    /// <param name="orderByExpression">The expression specifying the property to order by.</param>
    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    /// <summary>
    /// Applies descending ordering to the query results.
    /// </summary>
    /// <param name="orderByDescendingExpression">The expression specifying the property to order by in descending order.</param>
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        OrderByDescending = orderByDescendingExpression;
    }

    /// <summary>
    /// Applies grouping to the query results.
    /// </summary>
    /// <param name="groupByExpression">The expression specifying the property to group by.</param>
    protected void ApplyGroupBy(Expression<Func<T, object>> groupByExpression)
    {
        GroupBy = groupByExpression;
    }
}
