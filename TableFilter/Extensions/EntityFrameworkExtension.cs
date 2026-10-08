using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DynamicTableFilter;

/// <summary>
/// Provides extension methods for Entity Framework Core asynchronous execution.
/// </summary>
public static class EntityFrameworkExtension
{
    /// <summary>
    /// Applies dynamic filtering, sorting, and pagination, and asynchronously executes the query 
    /// to return a paginated result including the total count.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the queryable collection.</typeparam>
    /// <param name="query">The initial queryable collection.</param>
    /// <param name="pageRequest">The request object containing filter, sort, and pagination rules.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="PageResult{T}"/> containing total records count and paginated data.</returns>
    public static async Task<PageResult<T>> ApplyPageRequestAsync<T>(
        this IQueryable<T> query,
        FilterRequest pageRequest,
        CancellationToken cancellationToken = default)
    {
        var sortedFilteredQuery = query.ApplyPageRequest(pageRequest, ignoreSkipTake: true);
        
        int count = await sortedFilteredQuery.CountAsync(cancellationToken);
        
        int pageSize = pageRequest.PageSize > 0 ? pageRequest.PageSize : 10;
        if (pageSize > 1000) pageSize = 1000;
        
        int pageIndex = Math.Max(0, pageRequest.PageIndex);
        
        long skip = (long)pageIndex * pageSize;
        if (skip > int.MaxValue) skip = int.MaxValue;
        
        var finalQuery = sortedFilteredQuery.Skip((int)skip).Take(pageSize);
        var data = await finalQuery.ToListAsync(cancellationToken);
        
        return new PageResult<T>(count, data);
    }
}
