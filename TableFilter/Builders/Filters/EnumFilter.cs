using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DynamicTableFilter;

internal static class EnumFilter
{
    public static Expression Build(object filterValue, MemberExpression member)
    {
        if (FilterHelper.IsNullLikeFilterValue(filterValue))
            return Expression.Constant(true);

        if (filterValue is IEnumerable<object> enumerable)
        {
            var filtered = enumerable.Where(v => !FilterHelper.IsNullLikeFilterValue(v)).ToArray();
            if (filtered.Length == 0)
                return Expression.Constant(true);

            var enumValues = filtered
                .Select(value => Enum.ToObject(member.Type, Convert.ChangeType(value, Enum.GetUnderlyingType(member.Type))))
                .ToArray();

            var enumTypedArray = Array.CreateInstance(member.Type, enumValues.Length);
            Array.Copy(enumValues, enumTypedArray, enumValues.Length);

            MethodInfo containsMethod = typeof(Enumerable).GetMethods()
                .Single(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(member.Type);

            return Expression.Call(null, containsMethod, Expression.Constant(enumTypedArray), member);
        }

        var enumType = Nullable.GetUnderlyingType(member.Type) ?? member.Type;
        var enumValue = Enum.ToObject(enumType, filterValue);

        ConstantExpression constantEnum = Expression.Constant(enumValue, member.Type);
        return Expression.Equal(member, constantEnum);
    }
}
