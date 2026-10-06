using DynamicTableFilter;

namespace DynamicTableFilter.Tests;

/// <summary>
/// Tests for ExpressionBuilder.BuildPredicate — the core filtering engine.
/// These tests compile predicates and run them in-memory against List<T>.AsQueryable().
/// Note: EF.Functions.Like() throws in non-EF (in-memory) context,
/// so string filter tests are in a separate file that tests the expression tree structure.
/// </summary>
public class ExpressionBuilderTests
{
    private static readonly List<TestEntity> TestData =
    [
        new()
        {
            Name = "Alice", Age = 30, BigNumber = 1_000_000L, Price = 99.99, Salary = 5000m,
            Rating = 4.5f, IsActive = true, Status = TestStatus.Active,
            CreatedAt = new DateTime(2024, 1, 15), BirthDate = new DateOnly(1994, 5, 20),
            Tags = [1, 2, 3], CategoryIds = [10, 20], Score = 85, Discount = 10.5
        },
        new()
        {
            Name = "Bob", Age = 25, BigNumber = 2_000_000L, Price = 49.99, Salary = 3000m,
            Rating = 3.8f, IsActive = false, Status = TestStatus.Inactive,
            CreatedAt = new DateTime(2024, 3, 20), BirthDate = new DateOnly(1999, 8, 10),
            Tags = [2, 4], CategoryIds = [20, 30], Score = null, Discount = null
        },
        new()
        {
            Name = "Charlie", Age = 35, BigNumber = 500_000L, Price = 199.99, Salary = 8000m,
            Rating = 4.9f, IsActive = true, Status = TestStatus.Pending,
            CreatedAt = new DateTime(2024, 6, 1), BirthDate = new DateOnly(1989, 12, 1),
            Tags = [1, 5], CategoryIds = [10, 40], Score = 92, Discount = 5.0
        }
    ];

    private static List<TestEntity> ApplyFilter(string key, object value)
    {
        var request = new FilterRequest
        {
            PageIndex = 0,
            PageSize = 100,
            Filter = [new Filter { Key = key, Value = value }]
        };
        var predicate = ExpressionBuilder.BuildPredicate<TestEntity>(request);
        return TestData.AsQueryable().Where(predicate).ToList();
    }

    #region No Filters / Empty Filters

    [Fact]
    public void BuildPredicate_NoFilters_ReturnsAll()
    {
        var request = new FilterRequest { PageIndex = 0, PageSize = 100 };
        var predicate = ExpressionBuilder.BuildPredicate<TestEntity>(request);
        var result = TestData.AsQueryable().Where(predicate).ToList();
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void BuildPredicate_EmptyFilterList_ReturnsAll()
    {
        var request = new FilterRequest
        {
            PageIndex = 0, PageSize = 100,
            Filter = []
        };
        var predicate = ExpressionBuilder.BuildPredicate<TestEntity>(request);
        var result = TestData.AsQueryable().Where(predicate).ToList();
        Assert.Equal(3, result.Count);
    }

    #endregion

    #region Int Filters

    [Fact]
    public void Filter_Int_EqualValue_ReturnsMatch()
    {
        var result = ApplyFilter("Age", 30);
        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    [Fact]
    public void Filter_Int_MultipleValues_ReturnsMatches()
    {
        var result = ApplyFilter("Age", new List<object> { 25, 35 });
        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Name == "Bob");
        Assert.Contains(result, e => e.Name == "Charlie");
    }

    [Fact]
    public void Filter_NullableInt_WithValue_ReturnsMatch()
    {
        var result = ApplyFilter("Score", 85);
        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    #endregion

    #region Long Filters

    [Fact]
    public void Filter_Long_EqualValue_ReturnsMatch()
    {
        var result = ApplyFilter("BigNumber", 2_000_000L);
        Assert.Single(result);
        Assert.Equal("Bob", result[0].Name);
    }

    #endregion

    #region Double Filters

    [Fact]
    public void Filter_Double_EqualValue_ReturnsMatch()
    {
        var result = ApplyFilter("Price", 99.99);
        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    #endregion

    #region Decimal Filters

    [Fact]
    public void Filter_Decimal_EqualValue_ReturnsMatch()
    {
        var result = ApplyFilter("Salary", 5000m);
        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    #endregion

    #region Boolean Filters

    [Fact]
    public void Filter_Boolean_True_ReturnsActiveOnly()
    {
        var result = ApplyFilter("IsActive", true);
        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.True(e.IsActive));
    }

    [Fact]
    public void Filter_Boolean_False_ReturnsInactiveOnly()
    {
        var result = ApplyFilter("IsActive", false);
        Assert.Single(result);
        Assert.Equal("Bob", result[0].Name);
    }

    #endregion

    #region Enum Filters

    [Fact]
    public void Filter_Enum_SingleValue_ReturnsMatch()
    {
        var result = ApplyFilter("Status", (int)TestStatus.Active);
        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    [Fact]
    public void Filter_Enum_MultipleValues_ReturnsMatches()
    {
        var result = ApplyFilter("Status", new List<object> { (int)TestStatus.Active, (int)TestStatus.Pending });
        Assert.Equal(2, result.Count);
    }

    #endregion

    #region DateTime Filters

    [Fact]
    public void Filter_DateTime_MatchesDay()
    {
        var result = ApplyFilter("CreatedAt", "01.15.2024");
        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    #endregion

    #region DateOnly Filters

    [Fact]
    public void Filter_DateOnly_ExactMatch()
    {
        var result = ApplyFilter("BirthDate", "05.20.1994");
        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    #endregion

    #region From/To Range Filters

    [Fact]
    public void Filter_FromTo_AgeRange_ReturnsInRange()
    {
        var request = new FilterRequest
        {
            PageIndex = 0, PageSize = 100,
            Filter =
            [
                new Filter { Key = "Age.from", Value = 25 },
                new Filter { Key = "Age.to", Value = 31 }
            ]
        };
        var predicate = ExpressionBuilder.BuildPredicate<TestEntity>(request);
        var result = TestData.AsQueryable().Where(predicate).ToList();

        // Age >= 25 AND Age < 31 → Alice(30) and Bob(25)
        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Name == "Alice");
        Assert.Contains(result, e => e.Name == "Bob");
    }

    [Fact]
    public void Filter_FromTo_DateRange_ReturnsInRange()
    {
        var request = new FilterRequest
        {
            PageIndex = 0, PageSize = 100,
            Filter =
            [
                new Filter { Key = "CreatedAt.from", Value = "01.01.2024" },
                new Filter { Key = "CreatedAt.to", Value = "04.01.2024" }
            ]
        };
        var predicate = ExpressionBuilder.BuildPredicate<TestEntity>(request);
        var result = TestData.AsQueryable().Where(predicate).ToList();

        // CreatedAt >= Jan 1 AND CreatedAt < Apr 2 → Alice(Jan 15) and Bob(Mar 20)
        Assert.Equal(2, result.Count);
    }

    #endregion

    #region NumericArray Filters

    [Fact]
    public void Filter_IntArray_ContainsValue()
    {
        var result = ApplyFilter("Tags", 5);
        Assert.Single(result);
        Assert.Equal("Charlie", result[0].Name);
    }

    [Fact]
    public void Filter_ListInt_ContainsValue()
    {
        var result = ApplyFilter("CategoryIds", 30);
        Assert.Single(result);
        Assert.Equal("Bob", result[0].Name);
    }

    #endregion

    #region Multiple Filters (AND logic)

    [Fact]
    public void Filter_MultipleConditions_CombinedWithAnd()
    {
        var request = new FilterRequest
        {
            PageIndex = 0, PageSize = 100,
            Filter =
            [
                new Filter { Key = "IsActive", Value = true },
                new Filter { Key = "Age", Value = 35 }
            ]
        };
        var predicate = ExpressionBuilder.BuildPredicate<TestEntity>(request);
        var result = TestData.AsQueryable().Where(predicate).ToList();

        Assert.Single(result);
        Assert.Equal("Charlie", result[0].Name);
    }

    #endregion

    #region Null-like Filter Values Skipped

    [Fact]
    public void Filter_NullValue_IsSkipped_ReturnsAll()
    {
        var request = new FilterRequest
        {
            PageIndex = 0, PageSize = 100,
            Filter = [new Filter { Key = "Name", Value = null! }]
        };
        var predicate = ExpressionBuilder.BuildPredicate<TestEntity>(request);
        var result = TestData.AsQueryable().Where(predicate).ToList();
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Filter_EmptyStringValue_IsSkipped_ReturnsAll()
    {
        var request = new FilterRequest
        {
            PageIndex = 0, PageSize = 100,
            Filter = [new Filter { Key = "Name", Value = "  " }]
        };
        var predicate = ExpressionBuilder.BuildPredicate<TestEntity>(request);
        var result = TestData.AsQueryable().Where(predicate).ToList();
        Assert.Equal(3, result.Count);
    }

    #endregion
}
