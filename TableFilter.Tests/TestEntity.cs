namespace DynamicTableFilter.Tests;

public enum TestStatus
{
    Active = 0,
    Inactive = 1,
    Pending = 2
}

public class TestEntity
{
    // String
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }

    // Numeric types
    public int Age { get; set; }
    public int? Score { get; set; }
    public long BigNumber { get; set; }
    public long? NullableBigNumber { get; set; }
    public double Price { get; set; }
    public double? Discount { get; set; }
    public decimal Salary { get; set; }
    public decimal? Bonus { get; set; }
    public float Rating { get; set; }

    // Boolean
    public bool IsActive { get; set; }

    // Enum
    public TestStatus Status { get; set; }

    // Date types
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateOnly BirthDate { get; set; }
    public DateOnly? GraduationDate { get; set; }

    // Array types
    public int[] Tags { get; set; } = Array.Empty<int>();
    public List<int> CategoryIds { get; set; } = new();
}
