using System;
using System.Linq.Expressions;

namespace DynamicTableFilter;

internal static class FromToFilter
{
    public static Expression Build(MemberExpression memberExpression, object filterValue, bool isTo)
    {
        ConstantExpression constantExpression;

        if (memberExpression.Type == typeof(DateOnly) || memberExpression.Type == typeof(DateOnly?) ||
            memberExpression.Type == typeof(DateTime) || memberExpression.Type == typeof(DateTime?))
        {
            constantExpression = FilterHelper.DateTimeAndDateOnlyExpression(memberExpression.Type, filterValue, isTo);
        }
        else if (FilterHelper.IsNumericType(memberExpression.Type))
        {
            var targetType = Nullable.GetUnderlyingType(memberExpression.Type) ?? memberExpression.Type;
            var convertedValue = Convert.ChangeType(filterValue, targetType);
            constantExpression = Expression.Constant(convertedValue, memberExpression.Type);
        }
        else
        {
            throw new InvalidOperationException($"Property '{memberExpression.Member.Name}' is not of type for *from and to* filtering.");
        }

        return !isTo
            ? Expression.GreaterThanOrEqual(memberExpression, constantExpression)
            : Expression.LessThan(memberExpression, constantExpression);
    }
}
