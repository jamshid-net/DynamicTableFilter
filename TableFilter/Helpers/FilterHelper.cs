using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace DynamicTableFilter;

internal static class FilterHelper
{
    private static readonly HashSet<Type> NumericTypes =
    [
        typeof(int), typeof(int?),
        typeof(long), typeof(long?),
        typeof(double), typeof(double?),
        typeof(float), typeof(float?),
        typeof(decimal), typeof(decimal?),
        typeof(short), typeof(short?),
        typeof(byte), typeof(byte?)
    ];

    private static readonly HashSet<Type> NumericArrayTypes =
    [
        typeof(int[]), typeof(List<int>), typeof(int?[]), typeof(List<int?>),
        typeof(long[]), typeof(List<long>), typeof(long?[]), typeof(List<long?>)
    ];

    public static bool IsNullLikeFilterValue(object? value)
    {
        if (value is null)
            return true;

        if (value is string s)
            return string.IsNullOrWhiteSpace(s);

        if (value is JsonElement je)
        {
            if (je.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return true;

            if (je.ValueKind == JsonValueKind.String)
                return string.IsNullOrWhiteSpace(je.GetString());
        }

        if (value is JValue jv)
            return jv.Type == JTokenType.Null || (jv.Type == JTokenType.String && string.IsNullOrWhiteSpace(jv.ToString(CultureInfo.InvariantCulture)));

        if (value is IEnumerable<object> enumerable)
            return !enumerable.Any(v => !IsNullLikeFilterValue(v));

        return false;
    }

    public static bool IsNumericType(Type type) => NumericTypes.Contains(type);

    public static bool IsNumericArrayType(Type type) => NumericArrayTypes.Contains(type);

    public static bool IsNullableEnum(Type type)
    {
        Type? underlyingType = Nullable.GetUnderlyingType(type);
        return underlyingType is { IsEnum: true };
    }

    public static ConstantExpression DateTimeAndDateOnlyExpression(Type memberType, object? filterValue, bool isTo = false)
    {
        string dateOnlyFormat = "MM.dd.yyyy";
        var constant = filterValue switch
        {
            string dateString when memberType == typeof(DateOnly) || memberType == typeof(DateOnly?) =>
                DateOnly.TryParseExact(dateString, dateOnlyFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)
                    ? Expression.Constant(memberType == typeof(DateOnly) ? (isTo ? parsedDate.AddDays(1) : parsedDate) : (DateOnly?)(isTo ? parsedDate.AddDays(1) : parsedDate), memberType)
                    : throw new InvalidOperationException($"Cannot convert filter value '{filterValue}' to DateOnly."),

            string dateString when memberType == typeof(DateTime) || memberType == typeof(DateTime?) =>
                DateTime.TryParseExact(dateString, DateFormat.ReadDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDateTime)
                    ? Expression.Constant(
                        memberType == typeof(DateTime)
                            ? (isTo ? parsedDateTime.Date.AddDays(1) : parsedDateTime)
                            : (DateTime?)(isTo ? parsedDateTime.Date.AddDays(1) : parsedDateTime),
                        memberType)
                    : throw new InvalidOperationException($"Cannot convert filter value '{filterValue}' to DateTime."),

            DateOnly dateOnlyValue when memberType == typeof(DateOnly) || memberType == typeof(DateOnly?) =>
                Expression.Constant(memberType == typeof(DateOnly) ? (isTo ? dateOnlyValue.AddDays(1) : dateOnlyValue) : (DateOnly?)(isTo ? dateOnlyValue.AddDays(1) : dateOnlyValue), memberType),

            DateTime dateTimeValue when memberType == typeof(DateTime) || memberType == typeof(DateTime?) =>
                Expression.Constant(
                    memberType == typeof(DateTime)
                        ? (isTo ? dateTimeValue.Date.AddDays(1) : dateTimeValue)
                        : (DateTime?)(isTo ? dateTimeValue.Date.AddDays(1) : dateTimeValue),
                    memberType),

            _ => throw new InvalidOperationException($"Cannot convert filter value '{filterValue}' to a supported date type.")
        };
        return constant;
    }
}
