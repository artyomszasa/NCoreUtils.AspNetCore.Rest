using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace NCoreUtils.AspNetCore.Rest;

internal static class PropertyInfoExtensions
{
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Must have been preserved by caller")]
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "The condition is kept for future use and to make the logic explicit.")]
    public static LambdaExpression CreateSelector(this PropertyInfo property, Type? parameterType = null)
    {
        var eArgType = parameterType switch
        {
            null => property.ReflectedType,
            Type ptype => property.DeclaringType!.IsAssignableFrom(ptype)
                ? ptype
                : throw new InvalidOperationException($"{ptype} cannot be used as parameter type for property selector of {property.DeclaringType}.{property.Name}.")
        };
        var eArg = Expression.Parameter(eArgType ?? throw new InvalidOperationException("Unable to get parameter type."));
        return Expression.Lambda(Expression.Property(eArg, property), eArg);
    }
}