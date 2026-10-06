using System;

namespace DynamicTableFilter;

/// <summary>
/// Represents a single filter condition with a property key and a value to match against.
/// </summary>
public class Filter
{
    private string _key = string.Empty;

    /// <summary>
    /// The property name to filter on. Cannot be null or whitespace.
    /// Supports suffixes like ".from" and ".to" for range filtering.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when null or whitespace is assigned.</exception>
    public required string Key
    {
        get => _key;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(Key));
            _key = value;
        }
    }

    /// <summary>
    /// The value to filter by. Can be a single value or an array for multi-select filters.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when null is assigned.</exception>
    public required object Value { get; set; }
}
