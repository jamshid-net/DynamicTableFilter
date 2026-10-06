using DynamicTableFilter;

namespace DynamicTableFilter.Tests;

/// <summary>
/// Tests for QueryableExtension.ApplyPageRequest — sorting and pagination.
/// String filtering uses EF.Functions.Like() which doesn't work in-memory,
/// so these tests only exercise sorting and pagination with non-string filters.
/// </summary>
public class QueryableExtensionTests
{
    private static readonly List<TestEntity> TestData =
    [
        new() { Name = "Charlie", Age = 35, IsActive = true },
        new() { Name = "Alice", Age = 30, IsActive = true },
        new() { Name = "Bob", Age = 25, IsActive = false },
        new() { Name = "Diana", Age = 28, IsActive = true },
        new() { Name = "Eve", Age = 40, IsActive = false }
    ];

    #region Pagination

    [Fact]
    public void ApplyPageRequest_Page0_Size2_ReturnsFirst2()
    {
        var request = new FilterRequest { PageIndex = 0, PageSize = 2 };
        var result = TestData.AsQueryable().ApplyPageRequest(request).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ApplyPageRequest_Page1_Size2_ReturnsNext2()
    {
        var request = new FilterRequest { PageIndex = 1, PageSize = 2 };
        var result = TestData.AsQueryable().ApplyPageRequest(request).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ApplyPageRequest_LastPage_ReturnsRemaining()
    {
        var request = new FilterRequest { PageIndex = 2, PageSize = 2 };
        var result = TestData.AsQueryable().ApplyPageRequest(request).ToList();

        Assert.Single(result);
    }

    [Fact]
    public void ApplyPageRequest_IgnoreSkipTake_ReturnsAll()
    {
        var request = new FilterRequest { PageIndex = 0, PageSize = 2 };
        var result = TestData.AsQueryable().ApplyPageRequest(request, ignoreSkipTake: true).ToList();

        Assert.Equal(5, result.Count);
    }

    #endregion

    #region Sorting

    [Fact]
    public void ApplyPageRequest_SortByAge_Asc_ReturnsSorted()
    {
        var request = new FilterRequest
        {
            PageIndex = 0, PageSize = 100,
            Sort = [new Sort { Key = "Age", Value = SortEnum.Asc }]
        };
        var result = TestData.AsQueryable().ApplyPageRequest(request).ToList();

        Assert.Equal(25, result[0].Age);
        Assert.Equal(28, result[1].Age);
        Assert.Equal(30, result[2].Age);
        Assert.Equal(35, result[3].Age);
        Assert.Equal(40, result[4].Age);
    }

    [Fact]
    public void ApplyPageRequest_SortByAge_Desc_ReturnsSorted()
    {
        var request = new FilterRequest
        {
            PageIndex = 0, PageSize = 100,
            Sort = [new Sort { Key = "Age", Value = SortEnum.Desc }]
        };
        var result = TestData.AsQueryable().ApplyPageRequest(request).ToList();

        Assert.Equal(40, result[0].Age);
        Assert.Equal(35, result[1].Age);
        Assert.Equal(30, result[2].Age);
    }

    #endregion

    #region Filter + Sort + Pagination Combined

    [Fact]
    public void ApplyPageRequest_FilterAndSortAndPage_WorksTogether()
    {
        var request = new FilterRequest
        {
            PageIndex = 0, PageSize = 2,
            Filter = [new Filter { Key = "IsActive", Value = true }],
            Sort = [new Sort { Key = "Age", Value = SortEnum.Asc }]
        };
        var result = TestData.AsQueryable().ApplyPageRequest(request).ToList();

        // Active: Diana(28), Alice(30), Charlie(35) → sorted by Age Asc → first 2
        Assert.Equal(2, result.Count);
        Assert.Equal("Diana", result[0].Name);
        Assert.Equal("Alice", result[1].Name);
    }

    #endregion
}
