using System;
using System.Linq;
using System.Linq.Expressions;

namespace DynamicTableFilter;

/// <summary>
/// Provides extension methods for <see cref="IQueryable{T}"/> to easily apply dynamic filtering, sorting, and pagination.
/// </summary>
public static class QueryableExtension
{
    /// <summary>
    /// Applies dynamic filtering, sorting, and pagination to the given query based on the provided request model.
    /// It dynamically constructs expression trees for filtering and sorting, preventing the need for manual where/orderby clauses.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the queryable collection.</typeparam>
    /// <param name="query">The initial queryable collection.</param>
    /// <param name="pageRequest">The request object containing filter conditions, sorting configurations, and pagination parameters (PageIndex, PageSize).</param>
    /// <param name="ignoreSkipTake">If set to <c>true</c>, pagination (Skip and Take) will be bypassed, returning all filtered and sorted results.</param>
    /// <returns>A new <see cref="IQueryable{T}"/> with all filters, sorting, and pagination applied.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a filter cannot be applied due to unsupported data types or invalid property mapping.</exception>
    public static IQueryable<T> ApplyPageRequest<T>(
        this IQueryable<T> query, 
        FilterRequest pageRequest,
        bool ignoreSkipTake = false)
    {
        // Detect if this is an in-memory query (List.AsQueryable) vs a real EF Core DbSet query.
        bool isEntityFramework = query.Provider.GetType().Name != "EnumerableQuery`1";
        var predicate = ExpressionBuilder.BuildPredicate<T>(pageRequest, isEntityFramework);
        query = query.Where(predicate);

        // Apply sorting if required
        if (pageRequest.Sort is { Count: > 0 })
        {
            bool firstSort = true;
            foreach (var sort in pageRequest.Sort)
            {
                query = ApplySorting(query, sort, firstSort);
                firstSort = false;
            }
        }

        // Apply paging
        if (!ignoreSkipTake)
        {
            int pageSize = pageRequest.PageSize > 0 ? pageRequest.PageSize : 10;
            if (pageSize > 1000) pageSize = 1000;
            
            int pageIndex = Math.Max(0, pageRequest.PageIndex);
            
            long skip = (long)pageIndex * pageSize;
            if (skip > int.MaxValue) skip = int.MaxValue;
            
            query = query.Skip((int)skip)
                         .Take(pageSize);
        }

        return query;
    }

    private static IOrderedQueryable<T> ApplySorting<T>(IQueryable<T> query, Sort sort, bool firstSort = true)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        MemberExpression property;
        try
        {
            property = Expression.Property(parameter, sort.Key);
        }
        catch (ArgumentException)
        {
            throw new ArgumentException($"Sort property '{sort.Key}' was not found on type '{typeof(T).Name}'. Please ensure the sort key exactly matches the property name (case-sensitive).");
        }
        var lambda = Expression.Lambda(property, parameter);

        var methodName = sort.Value switch
        {
            SortEnum.Asc => firstSort ? "OrderBy" : "ThenBy",
            SortEnum.Desc => firstSort ? "OrderByDescending" : "ThenByDescending",
            _ => firstSort ? "OrderBy" : "ThenBy"
        };

        var method = typeof(Queryable)
            .GetMethods()
            .Single(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), property.Type);

        return (IOrderedQueryable<T>)method.Invoke(null, new object[] { query, lambda })!;
    }
}




