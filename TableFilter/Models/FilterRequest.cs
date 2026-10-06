using System;
using System.Collections.Generic;

namespace DynamicTableFilter;

/// <summary>
/// Represents a request for filtered, sorted, and paginated data.
/// </summary>
public class FilterRequest
{
    private int _pageIndex;
    private int _pageSize = 10;

    /// <summary>
    /// Zero-based page index. Must be non-negative.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a negative value is assigned.</exception>
    public int PageIndex
    {
        get => _pageIndex;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(PageIndex));
            _pageIndex = value;
        }
    }

    /// <summary>
    /// Number of items per page. Must be greater than zero.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when zero or a negative value is assigned.</exception>
    public int PageSize
    {
        get => _pageSize;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(PageSize));
            _pageSize = value;
        }
    }

    /// <summary>
    /// Optional list of sorting rules to apply, in order of priority.
    /// </summary>
    public List<Sort>? Sort { get; set; }

    /// <summary>
    /// Optional list of filter conditions to apply. All conditions are combined with AND logic.
    /// </summary>
    public List<Filter>? Filter { get; set; }
}
