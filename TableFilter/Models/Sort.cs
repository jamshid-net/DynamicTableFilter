using System;

namespace DynamicTableFilter;

/// <summary>
/// Represents a single sorting rule with a property key and sort direction.
/// </summary>
public class Sort
{
    private string _key = string.Empty;

    /// <summary>
    /// The property name to sort by. Cannot be null or whitespace.
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
    /// The sort direction (ascending or descending).
    /// </summary>
    public SortEnum Value { get; set; } = SortEnum.Asc;
}
