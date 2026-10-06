using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DynamicTableFilter;

internal static class NumericFilter
{
    public static Expression Build(object filterValue, MemberExpression member)
    {
        if (FilterHelper.IsNullLikeFilterValue(filterValue))
            return Expression.Constant(true);

        if (filterValue is IEnumerable<object> numericArray)
        {
            var filteredValues = numericArray.Where(v => !FilterHelper.IsNullLikeFilterValue(v)).ToList();
            if (filteredValues.Count == 0)
                return Expression.Constant(true);

            var genericListType = typeof(List<>).MakeGenericType(member.Type);
            var numberList = Activator.CreateInstance(genericListType);
            var addMethod = genericListType.GetMethod("Add");
            
            var targetType = Nullable.GetUnderlyingType(member.Type) ?? member.Type;

            foreach (var value in filteredValues)
            {
                var convertedValue = Convert.ChangeType(value, targetType);
                addMethod?.Invoke(numberList, new[] { convertedValue });
            }

            MethodInfo containsMethod = genericListType.GetMethod("Contains", new[] { member.Type })!;
            var listConstant = Expression.Constant(numberList);
            return Expression.Call(listConstant, containsMethod, member);
        }

        var scalarTargetType = Nullable.GetUnderlyingType(member.Type) ?? member.Type;
        var nonArrayFilterValue = Convert.ChangeType(filterValue, scalarTargetType);
        var constant = Expression.Constant(nonArrayFilterValue, scalarTargetType);

        if (member.Type != scalarTargetType)
        {
            return Expression.Equal(member, Expression.Convert(constant, member.Type));
        }

        return Expression.Equal(member, constant);
    }
}
