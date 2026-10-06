using System;
using System.Linq.Expressions;

namespace DynamicTableFilter;

internal static class BooleanFilter
{
    public static Expression Build(object filterValue, MemberExpression member)
    {
        if (FilterHelper.IsNullLikeFilterValue(filterValue))
            return Expression.Constant(true);

        if (filterValue is not bool boolValue)
            boolValue = Convert.ToBoolean(filterValue);

        ConstantExpression constantBoolean = Expression.Constant(boolValue, member.Type);
        return Expression.Equal(member, constantBoolean);
    }
}
