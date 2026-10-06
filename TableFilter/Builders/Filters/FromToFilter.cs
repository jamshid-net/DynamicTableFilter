using System;
using System.Linq.Expressions;

namespace DynamicTableFilter;

internal static class FromToFilter
{
    public static Expression Build(ParameterExpression param, Filter filter)
    {
        var propertyKey = filter.Key.Split('.')[0];
        var memberExpression = Expression.Property(param, propertyKey);

        ConstantExpression constantExpression;

        if (memberExpression.Type == typeof(DateOnly) || memberExpression.Type == typeof(DateOnly?) ||
            memberExpression.Type == typeof(DateTime) || memberExpression.Type == typeof(DateTime?))
        {
            bool isTo = filter.Key.EndsWith(".to", StringComparison.OrdinalIgnoreCase);
            constantExpression = FilterHelper.DateTimeAndDateOnlyExpression(memberExpression.Type, filter.Value, isTo);
        }
        else if (FilterHelper.IsNumericType(memberExpression.Type))
        {
            var targetType = Nullable.GetUnderlyingType(memberExpression.Type) ?? memberExpression.Type;
            var convertedValue = Convert.ChangeType(filter.Value, targetType);
            constantExpression = Expression.Constant(convertedValue, memberExpression.Type);
        }
        else
        {
            throw new InvalidOperationException($"Property '{memberExpression.Member.Name}' is not of type for *from and to* filtering.");
        }

        return filter.Key.EndsWith(".from", StringComparison.OrdinalIgnoreCase)
            ? Expression.GreaterThanOrEqual(memberExpression, constantExpression)
            : Expression.LessThan(memberExpression, constantExpression);
    }
}
