using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace DynamicTableFilter;

internal static class StringFilter
{
    public static Expression Build(object filterValue, MemberExpression member)
    {
        if (FilterHelper.IsNullLikeFilterValue(filterValue))
            return Expression.Constant(true);

        if (filterValue is IEnumerable<object> stringArray)
        {
            var stringList = stringArray
                .Select(value => value is JValue jValue ?
                    jValue.ToString(CultureInfo.InvariantCulture) :
                    value.ToString()).ToList();

            var conditions = stringList.Select(str =>
                CreateContainsExpression(member, str!)
            ).ToList();

            Expression combinedOrCondition = conditions.First();
            foreach (var condition in conditions.Skip(1))
            {
                combinedOrCondition = Expression.OrElse(combinedOrCondition, condition);
            }

            return combinedOrCondition;
        }
        else
        {
            string? filterString = filterValue.ToString();

            if (string.IsNullOrWhiteSpace(filterString))
                return Expression.Constant(true);

            return CreateContainsExpression(member, filterString);
        }
    }

    /// <summary>
    /// Creates an EF.Functions.Like() expression for database-agnostic case-insensitive pattern matching.
    /// EF Core translates this to the appropriate SQL LIKE for each provider (SQL Server, PostgreSQL, MySQL, etc.).
    /// </summary>
    private static Expression CreateContainsExpression(MemberExpression member, string filterString)
    {
        // EF.Functions.Like(member, "%value%") — works across all EF Core database providers.
        // SQL Server: LIKE is case-insensitive by default collation.
        // PostgreSQL: LIKE is case-sensitive, so we apply LOWER() on both sides as fallback.
        // MySQL: LIKE is case-insensitive by default collation.
        // Using ToLower + Like pattern ensures consistent case-insensitive behavior everywhere.

        MethodInfo toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
        var lowerMember = Expression.Call(member, toLowerMethod);

        var likePattern = $"%{filterString.ToLower()}%";

        // Get EF.Functions.Like(string, string)
        MethodInfo likeMethod = typeof(DbFunctionsExtensions)
            .GetMethod("Like", new[] { typeof(DbFunctions), typeof(string), typeof(string) })!;

        // EF.Functions is accessed via EF.Functions static property
        var efFunctionsProperty = Expression.Property(null, typeof(EF), nameof(EF.Functions));

        return Expression.Call(
            null,
            likeMethod,
            efFunctionsProperty,
            lowerMember,
            Expression.Constant(likePattern)
        );
    }
}
