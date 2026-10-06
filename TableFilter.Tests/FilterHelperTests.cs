using System.Text.Json;
using DynamicTableFilter;
using Newtonsoft.Json.Linq;

namespace DynamicTableFilter.Tests;

public class FilterHelperTests
{
    #region IsNullLikeFilterValue

    [Fact]
    public void IsNullLike_Null_ReturnsTrue()
    {
        Assert.True(FilterHelper.IsNullLikeFilterValue(null));
    }

    [Fact]
    public void IsNullLike_EmptyString_ReturnsTrue()
    {
        Assert.True(FilterHelper.IsNullLikeFilterValue(""));
    }

    [Fact]
    public void IsNullLike_WhitespaceString_ReturnsTrue()
    {
        Assert.True(FilterHelper.IsNullLikeFilterValue("   "));
    }

    [Fact]
    public void IsNullLike_ValidString_ReturnsFalse()
    {
        Assert.False(FilterHelper.IsNullLikeFilterValue("hello"));
    }

    [Fact]
    public void IsNullLike_Integer_ReturnsFalse()
    {
        Assert.False(FilterHelper.IsNullLikeFilterValue(42));
    }

    [Fact]
    public void IsNullLike_JsonElementNull_ReturnsTrue()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("null");
        Assert.True(FilterHelper.IsNullLikeFilterValue(json));
    }

    [Fact]
    public void IsNullLike_JsonElementString_ReturnsFalse()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("\"hello\"");
        Assert.False(FilterHelper.IsNullLikeFilterValue(json));
    }

    [Fact]
    public void IsNullLike_JsonElementEmptyString_ReturnsTrue()
    {
        var json = JsonSerializer.Deserialize<JsonElement>("\"\"");
        Assert.True(FilterHelper.IsNullLikeFilterValue(json));
    }

    [Fact]
    public void IsNullLike_JValueNull_ReturnsTrue()
    {
        Assert.True(FilterHelper.IsNullLikeFilterValue(JValue.CreateNull()));
    }

    [Fact]
    public void IsNullLike_JValueString_ReturnsFalse()
    {
        Assert.False(FilterHelper.IsNullLikeFilterValue(new JValue("test")));
    }

    [Fact]
    public void IsNullLike_EmptyEnumerable_ReturnsTrue()
    {
        Assert.True(FilterHelper.IsNullLikeFilterValue(new List<object>()));
    }

    [Fact]
    public void IsNullLike_EnumerableWithAllNulls_ReturnsTrue()
    {
        var list = new List<object> { null!, "  ", "" };
        Assert.True(FilterHelper.IsNullLikeFilterValue(list));
    }

    [Fact]
    public void IsNullLike_EnumerableWithValidValues_ReturnsFalse()
    {
        var list = new List<object> { null!, "hello" };
        Assert.False(FilterHelper.IsNullLikeFilterValue(list));
    }

    #endregion

    #region IsNumericType

    [Theory]
    [InlineData(typeof(int), true)]
    [InlineData(typeof(int?), true)]
    [InlineData(typeof(long), true)]
    [InlineData(typeof(long?), true)]
    [InlineData(typeof(double), true)]
    [InlineData(typeof(double?), true)]
    [InlineData(typeof(float), true)]
    [InlineData(typeof(float?), true)]
    [InlineData(typeof(decimal), true)]
    [InlineData(typeof(decimal?), true)]
    [InlineData(typeof(short), true)]
    [InlineData(typeof(byte), true)]
    [InlineData(typeof(string), false)]
    [InlineData(typeof(bool), false)]
    [InlineData(typeof(DateTime), false)]
    public void IsNumericType_ReturnsExpected(Type type, bool expected)
    {
        Assert.Equal(expected, FilterHelper.IsNumericType(type));
    }

    #endregion

    #region IsNullableEnum

    [Fact]
    public void IsNullableEnum_NullableEnumType_ReturnsTrue()
    {
        Assert.True(FilterHelper.IsNullableEnum(typeof(TestStatus?)));
    }

    [Fact]
    public void IsNullableEnum_NonNullableEnumType_ReturnsFalse()
    {
        Assert.False(FilterHelper.IsNullableEnum(typeof(TestStatus)));
    }

    [Fact]
    public void IsNullableEnum_RegularType_ReturnsFalse()
    {
        Assert.False(FilterHelper.IsNullableEnum(typeof(int)));
    }

    #endregion
}
