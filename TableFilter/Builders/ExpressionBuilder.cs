using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Collections.Generic;

namespace DynamicTableFilter;

public static class ExpressionBuilder
{
    public static Expression<Func<T, bool>> BuildPredicate<T>(FilterRequest pageRequest)
    {
        if (pageRequest.Filter == null || !pageRequest.Filter.Any())
        {
            return x => true;
        }

        var validFilters = pageRequest.Filter
            .Where(f => !FilterHelper.IsNullLikeFilterValue(f.Value))
            .ToList();

        if (validFilters.Count == 0)
        {
            return x => true;
        }

        ParameterExpression param = Expression.Parameter(typeof(T), "x");
        Expression combined = validFilters
            .Select(filter => BuildSinglePredicate<T>(param, filter))
            .Aggregate((current, predicate) => Expression.AndAlso(current, predicate));

        return Expression.Lambda<Func<T, bool>>(combined, param);
    }

    private static Expression BuildSinglePredicate<T>(ParameterExpression param, Filter filter)
    {
        bool isFromTo = filter.Key.EndsWith(".from", StringComparison.OrdinalIgnoreCase) ||
                        filter.Key.EndsWith(".to", StringComparison.OrdinalIgnoreCase);

        string propertyKey = isFromTo ? filter.Key.Split('.')[0] : filter.Key;
        
        MemberExpression member;
        try
        {
            if (propertyKey.Contains('.'))
            {
                var parts = propertyKey.Split('.');
                Expression current = param;
                foreach (var part in parts)
                {
                    current = Expression.Property(current, part);
                }
                member = (MemberExpression)current;
            }
            else
            {
                member = Expression.Property(param, propertyKey);
            }
        }
        catch (ArgumentException)
        {
            throw new ArgumentException($"Property '{propertyKey}' was not found on type '{typeof(T).Name}'. Please ensure the filter key exactly matches the property name (case-sensitive).");
        }

        object filterValue = filter.Value;

        if (filterValue is JsonElement jsonElement)
        {
            try
            {
                filterValue = ConvertJsonElement(jsonElement, member.Type);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid value provided for property '{propertyKey}'. Expected type is '{member.Type.Name}', but the provided value could not be converted. Error: {ex.Message}", ex);
            }
        }

        if (isFromTo)
        {
            bool isTo = filter.Key.EndsWith(".to", StringComparison.OrdinalIgnoreCase);
            return FromToFilter.Build(member, filterValue, isTo);
        }

        if (member.Type.IsEnum || FilterHelper.IsNullableEnum(member.Type))
        {
            return EnumFilter.Build(filterValue, member);
        }

        if (member.Type == typeof(DateTime) || member.Type == typeof(DateTime?))
        {
            return DateTimeFilter.Build(filterValue, member);
        }

        if (member.Type == typeof(DateOnly) || member.Type == typeof(DateOnly?))
        {
            return DateOnlyFilter.Build(filterValue, member);
        }

        if (member.Type == typeof(string))
        {
            return StringFilter.Build(filterValue, member);
        }

        if (FilterHelper.IsNumericType(member.Type))
        {
            return NumericFilter.Build(filterValue, member);
        }
        if (FilterHelper.IsNumericArrayType(member.Type))
        {
            return NumericArrayFilter.Build(filterValue, member);
        }
        if (member.Type == typeof(bool) || member.Type == typeof(bool?))
        {
            return BooleanFilter.Build(filterValue, member);
        }

        throw new NotSupportedException($"Filter value type {filterValue.GetType()} is not supported.");
    }

    private static object ConvertJsonElement(JsonElement jsonElement, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType.IsEnum)
        {
            if (jsonElement.ValueKind == JsonValueKind.String)
            {
                return Enum.Parse(underlyingType, jsonElement.GetString()!, ignoreCase: true);
            }
            if (jsonElement.ValueKind == JsonValueKind.Number)
            {
                var enumUnderlying = Enum.GetUnderlyingType(underlyingType);
                return Type.GetTypeCode(enumUnderlying) switch
                {
                    TypeCode.SByte => jsonElement.GetSByte(),
                    TypeCode.Byte => jsonElement.GetByte(),
                    TypeCode.Int16 => jsonElement.GetInt16(),
                    TypeCode.UInt16 => jsonElement.GetUInt16(),
                    TypeCode.Int32 => jsonElement.GetInt32(),
                    TypeCode.UInt32 => jsonElement.GetUInt32(),
                    TypeCode.Int64 => jsonElement.GetInt64(),
                    TypeCode.UInt64 => jsonElement.GetUInt64(),
                    _ => jsonElement.GetInt32()
                };
            }
        }

        return jsonElement.ValueKind switch
        {
            JsonValueKind.Array => jsonElement.EnumerateArray()
                .Select(item => ConvertJsonElement(item, targetType))
                .ToList(),
            JsonValueKind.String => underlyingType == typeof(DateOnly)
                ? DateOnly.Parse(jsonElement.GetString()!)
                : jsonElement.GetString()!,
            JsonValueKind.Number => Type.GetTypeCode(underlyingType) switch
            {
                TypeCode.Int16 => jsonElement.GetInt16(),
                TypeCode.Int32 => jsonElement.GetInt32(),
                TypeCode.Int64 => jsonElement.GetInt64(),
                TypeCode.Double => jsonElement.GetDouble(),
                TypeCode.Single => jsonElement.GetSingle(),
                TypeCode.Decimal => jsonElement.GetDecimal(),
                TypeCode.Byte => jsonElement.GetByte(),
                TypeCode.Boolean => jsonElement.GetInt32() switch
                {
                    1 => true,
                    0 => false,
                    _ => throw new FormatException($"Invalid numeric value for boolean property. Expected 0 or 1, but received {jsonElement.GetInt32()}.")
                },
                _ => throw new InvalidOperationException($"Cannot convert JSON number to {targetType}."),
            },
            JsonValueKind.True or JsonValueKind.False => jsonElement.GetBoolean(),
            // Null is valid for nullable target types - the caller ensures targetType is Nullable<T>
            JsonValueKind.Null when Nullable.GetUnderlyingType(targetType) != null => null!,
            _ => throw new NotSupportedException($"JSON value kind {jsonElement.ValueKind} is not supported."),
        };
    }
}
