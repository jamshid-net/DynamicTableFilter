using System;
using System.Linq.Expressions;

namespace DynamicTableFilter;

internal static class BooleanFilter
{
    public static Expression Build(object filterValue, MemberExpression member)
    {
        if (FilterHelper.IsNullLikeFilterValue(filterValue))
            return Expression.Constant(true);

        bool boolValue;
        if (filterValue is bool b)
        {
            boolValue = b;
        }
        else
        {
            var strVal = filterValue.ToString()?.Trim();
            if (strVal == "1") boolValue = true;
            else if (strVal == "0") boolValue = false;
            else boolValue = Convert.ToBoolean(filterValue);
        }

        ConstantExpression constantBoolean = Expression.Constant(boolValue, member.Type);
        return Expression.Equal(member, constantBoolean);
    }
}
