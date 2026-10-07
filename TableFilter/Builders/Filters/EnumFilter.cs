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

        var enumType = Nullable.GetUnderlyingType(member.Type) ?? member.Type;

        if (filterValue is IEnumerable<object> enumerable)
        {
            var filtered = enumerable.Where(v => !FilterHelper.IsNullLikeFilterValue(v)).ToArray();
            if (filtered.Length == 0)
                return Expression.Constant(true);

            var enumValues = filtered
                .Select(value => value.GetType() == enumType
                    ? value
                    : Enum.ToObject(enumType, Convert.ChangeType(value, Enum.GetUnderlyingType(enumType))))
                .ToArray();

            var enumTypedArray = Array.CreateInstance(enumType, enumValues.Length);
            Array.Copy(enumValues, enumTypedArray, enumValues.Length);

            MethodInfo containsMethod = typeof(Enumerable).GetMethods()
                .Single(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(enumType);

            // If the member is nullable, we need to coalesce it or handle the contains differently, 
            // but normally EF/Enumerable translates Contains well on nullable arrays if array is of same underlying type.
            // But just in case, casting the member to non-nullable helps memory provider or we use Nullable<> Array.
            var containsCall = member.Type != enumType 
                ? Expression.Call(null, containsMethod, Expression.Constant(enumTypedArray), Expression.Convert(member, enumType))
                : Expression.Call(null, containsMethod, Expression.Constant(enumTypedArray), member);
                
            if (member.Type != enumType)
            {
                var notNull = Expression.NotEqual(member, Expression.Constant(null, member.Type));
                return Expression.AndAlso(notNull, containsCall);
            }
            return containsCall;
        }

        object enumValue;
        if (filterValue.GetType() == enumType)
        {
            enumValue = filterValue;
        }
        else
        {
            enumValue = Enum.ToObject(enumType, Convert.ChangeType(filterValue, Enum.GetUnderlyingType(enumType)));
        }

        ConstantExpression constantEnum = Expression.Constant(enumValue, enumType);
        
        if (member.Type != enumType)
        {
            var notNull = Expression.NotEqual(member, Expression.Constant(null, member.Type));
            var equal = Expression.Equal(Expression.Convert(member, enumType), constantEnum);
            return Expression.AndAlso(notNull, equal);
        }

        return Expression.Equal(member, constantEnum);
    }
}
