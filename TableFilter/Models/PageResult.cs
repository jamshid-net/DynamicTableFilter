using System.Collections.Generic;

namespace DynamicTableFilter;

/// <summary>
/// Represents a paginated result containing the total count and the page data.
/// </summary>
public class PageResult<T>
{
    public int TotalCount { get; }
    public IReadOnlyList<T> Data { get; }

    public PageResult(int totalCount, IReadOnlyList<T> data)
    {
        TotalCount = totalCount;
        Data = data;
    }
}
