using DynamicTableFilter;

namespace DynamicTableFilter.Tests;

public class ModelValidationTests
{
    [Fact]
    public void Filter_NullKey_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Filter { Key = null!, Value = "test" });
    }

    [Fact]
    public void Filter_WhitespaceKey_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Filter { Key = "   ", Value = "test" });
    }

    [Fact]
    public void Filter_EmptyKey_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Filter { Key = "", Value = "test" });
    }

    [Fact]
    public void Filter_ValidKey_Works()
    {
        var filter = new Filter { Key = "Name", Value = "John" };
        Assert.Equal("Name", filter.Key);
        Assert.Equal("John", filter.Value);
    }

    [Fact]
    public void Sort_NullKey_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Sort { Key = null! });
    }

    [Fact]
    public void Sort_WhitespaceKey_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Sort { Key = "  " });
    }

    [Fact]
    public void Sort_DefaultDirection_IsAsc()
    {
        var sort = new Sort { Key = "Name" };
        Assert.Equal(SortEnum.Asc, sort.Value);
    }
}
