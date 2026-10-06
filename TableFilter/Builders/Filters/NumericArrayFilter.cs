using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DynamicTableFilter;

internal static class NumericArrayFilter
{
    public static Expression Build(object filterValue, MemberExpression member)
    {
        // Determine the element type of the array/list (e.g., int from int[] or List<int>)
        var elementType = member.Type.IsArray
            ? member.Type.GetElementType()!
            : member.Type.GetGenericArguments().First();

        var underlyingElementType = Nullable.GetUnderlyingType(elementType) ?? elementType;

        var convertedValue = Convert.ChangeType(filterValue, underlyingElementType);
        var constant = Expression.Constant(convertedValue, underlyingElementType);

        MethodInfo containsMethod = typeof(Enumerable)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .MakeGenericMethod(underlyingElementType);

        // If the element type is nullable (e.g. int?), cast the value
        Expression valueExpression = elementType != underlyingElementType
            ? Expression.Convert(constant, elementType)
            : constant;

        return Expression.Call(containsMethod, member, valueExpression);
    }
}
