using LiveEventService.Core.Common;
using Microsoft.EntityFrameworkCore;

namespace LiveEventService.Infrastructure.Data;

/// <summary>
/// Translates an <see cref="ISpecification{T}"/> into an EF Core <see cref="IQueryable{T}"/>
/// by applying criteria, includes, ordering, grouping, and paging.
/// </summary>
/// <typeparam name="T">The type of entity this evaluator works with.</typeparam>
public static class SpecificationEvaluator<T> where T : Entity
{
    /// <summary>
    /// Applies the specification to the provided base query.
    /// </summary>
    /// <param name="inputQuery">Base query over the entity set.</param>
    /// <param name="specification">Specification to apply.</param>
    /// <returns>The composed query.</returns>
    public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T> specification)
    {
        var query = inputQuery;

        // Modify the IQueryable using the specification's criteria expression
        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // Includes all expression-based includes
        query = specification.Includes
            .Aggregate(query, (current, include) => current.Include(include));

        // Include any string-based include statements
        query = specification.IncludeStrings
            .Aggregate(query, (current, include) => current.Include(include));

        // Apply ordering if expressions are set
        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        // Apply grouping if expression is set
        if (specification.GroupBy != null)
        {
            query = query.GroupBy(specification.GroupBy).SelectMany(x => x);
        }

        // Apply paging if enabled
        if (specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip)
                         .Take(specification.Take);
        }

        return query;
    }
}
