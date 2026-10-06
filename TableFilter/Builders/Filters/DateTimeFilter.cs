using System;
using System.Globalization;
using System.Linq.Expressions;

namespace DynamicTableFilter;

internal static class DateTimeFilter
{
    public static Expression Build(object filterValue, MemberExpression member)
    {
        if (filterValue is string strFilter)
        {
            DateTime parsedDateTime = DateTime.ParseExact(
                strFilter,
                DateFormat.ReadDateFormats,
                null,
                DateTimeStyles.None);

            DateTime rangeStart;
            DateTime rangeEnd;

            if (parsedDateTime is { Hour: 0, Minute: 0, Second: 0 })
            {
                rangeStart = new DateTime(parsedDateTime.Year, parsedDateTime.Month, parsedDateTime.Day, 0, 0, 0);
                rangeEnd = rangeStart.AddDays(1).AddSeconds(-1);
            }
            else
            {
                rangeStart = new DateTime(parsedDateTime.Year, parsedDateTime.Month, parsedDateTime.Day, parsedDateTime.Hour, parsedDateTime.Minute, 0);
                rangeEnd = rangeStart.AddSeconds(59);
            }

            var isNullable = member.Type == typeof(DateTime?);

            ConstantExpression constantRangeStart = Expression.Constant(rangeStart, typeof(DateTime));
            ConstantExpression constantRangeEnd = Expression.Constant(rangeEnd, typeof(DateTime));

            Expression memberExpression = isNullable
                ? Expression.Convert(member, typeof(DateTime))
                : member;

            var greaterThanOrEqual = Expression.GreaterThanOrEqual(memberExpression, constantRangeStart);
            var lessThanOrEqual = Expression.LessThanOrEqual(memberExpression, constantRangeEnd);

            var rangeComparison = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);

            if (isNullable)
            {
                var hasValueProperty = Expression.Property(member, "HasValue");
                var valueProperty = Expression.Property(member, "Value");

                var nullCheck = Expression.NotEqual(hasValueProperty, Expression.Constant(false));
                var valueComparison = Expression.AndAlso(
                    Expression.GreaterThanOrEqual(valueProperty, constantRangeStart),
                    Expression.LessThanOrEqual(valueProperty, constantRangeEnd)
                );

                rangeComparison = Expression.AndAlso(nullCheck, valueComparison);
            }

            return rangeComparison;
        }

        throw new ArgumentException("Filter value must be a string in valid date formats.");
    }
}
