using System;
using System.Globalization;
using System.Linq.Expressions;

namespace DynamicTableFilter;

internal static class DateOnlyFilter
{
    public static Expression Build(object filterValue, MemberExpression member)
    {
        if (filterValue is string strFilter)
        {
            DateOnly parsedDateOnly = DateOnly.ParseExact(strFilter, "MM.dd.yyyy", CultureInfo.InvariantCulture);
            ConstantExpression constantDateOnly = Expression.Constant(parsedDateOnly, typeof(DateOnly));

            var isNullable = member.Type == typeof(DateOnly?);

            if (isNullable)
            {
                var hasValueProperty = Expression.Property(member, "HasValue");
                var nullCheck = Expression.Equal(hasValueProperty, Expression.Constant(true));
                var valueProperty = Expression.Property(member, "Value");
                var valueComparison = Expression.Equal(valueProperty, constantDateOnly);
                return Expression.AndAlso(nullCheck, valueComparison);
            }
            else
            {
                return Expression.Equal(member, constantDateOnly);
            }
        }

        throw new ArgumentException("Filter value must be a string in 'MM.dd.yyyy' format.");
    }
}
