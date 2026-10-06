using DynamicTableFilter;

namespace DynamicTableFilter.Tests;

public class FilterRequestTests
{
    [Fact]
    public void PageIndex_NegativeValue_ThrowsArgumentOutOfRangeException()
    {
        var request = new FilterRequest();
        Assert.Throws<ArgumentOutOfRangeException>(() => request.PageIndex = -1);
    }

    [Fact]
    public void PageIndex_Zero_IsValid()
    {
        var request = new FilterRequest { PageIndex = 0 };
        Assert.Equal(0, request.PageIndex);
    }

    [Fact]
    public void PageSize_Zero_ThrowsArgumentOutOfRangeException()
    {
        var request = new FilterRequest();
        Assert.Throws<ArgumentOutOfRangeException>(() => request.PageSize = 0);
    }

    [Fact]
    public void PageSize_NegativeValue_ThrowsArgumentOutOfRangeException()
    {
        var request = new FilterRequest();
        Assert.Throws<ArgumentOutOfRangeException>(() => request.PageSize = -5);
    }

    [Fact]
    public void PageSize_DefaultValue_IsTen()
    {
        var request = new FilterRequest();
        Assert.Equal(10, request.PageSize);
    }

    [Fact]
    public void PageSize_PositiveValue_IsValid()
    {
        var request = new FilterRequest { PageSize = 50 };
        Assert.Equal(50, request.PageSize);
    }
}
