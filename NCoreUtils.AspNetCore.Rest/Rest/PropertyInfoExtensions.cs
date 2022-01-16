using System;
using System.Linq.Expressions;
using System.Reflection;

namespace NCoreUtils.AspNetCore.Rest
{
    static class PropertyInfoExtensions
    {
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
}