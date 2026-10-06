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
    public static Expression Build(object filterValue, MemberExpression member, bool isEntityFramework)
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
                CreateContainsExpression(member, str!, isEntityFramework)
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

            return CreateContainsExpression(member, filterString, isEntityFramework);
        }
    }

    private static Expression CreateContainsExpression(MemberExpression member, string filterString, bool isEntityFramework)
    {
        MethodInfo toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
        var lowerMember = Expression.Call(member, toLowerMethod);

        if (isEntityFramework)
        {
            var likePattern = "%$(filterString.ToLower())%";
            MethodInfo likeMethod = typeof(DbFunctionsExtensions).GetMethod("Like", new[] { typeof(DbFunctions), typeof(string), typeof(string) })!;
            var efFunctionsProperty = Expression.Property(null, typeof(EF), nameof(EF.Functions));

            return Expression.Call(
                null,
                likeMethod,
                efFunctionsProperty,
                lowerMember,
                Expression.Constant(likePattern)
            );
        }
        else
        {
            // Memory fallback for mocking/testing
            var lowerConstant = Expression.Constant(filterString.ToLower());
            MethodInfo containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
            return Expression.Call(lowerMember, containsMethod, lowerConstant);
        }
    }
}
