using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace NCoreUtils.AspNetCore.Rest
{
    public class DefaultDefaultOrderProperty
    {
        static ConcurrentDictionary<Type, OrderByProperty> _cache = new ConcurrentDictionary<Type, OrderByProperty>();

        static Func<Type, OrderByProperty> _factory = Create;

        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Ensured by caller.")]
        private static OrderByProperty Create(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo? updatedProperty = null;
            PropertyInfo? createdProperty = null;
            PropertyInfo? idProperty = null;
            foreach (var property in properties)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals("Updated", property.Name))
                {
                    updatedProperty = property;
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals("Created", property.Name))
                {
                    createdProperty = property;
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals("Id", property.Name))
                {
                    idProperty = property;
                }
            }
            if (null != updatedProperty)
            {
                return new OrderByProperty(updatedProperty, true);
            }
            if (null != createdProperty)
            {
                return new OrderByProperty(createdProperty, true);
            }
            if (null != idProperty)
            {
                return new OrderByProperty(idProperty, false);
            }
            return new OrderByProperty(properties.First(), false);
        }

        protected DefaultDefaultOrderProperty() { }

        protected internal static OrderByProperty GetDefaultOrderByProperty([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type)
            => _cache.GetOrAdd(type, _factory);
    }

    public class DefaultDefaultOrderProperty<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] T>
        : DefaultDefaultOrderProperty, IDefaultOrderProperty<T>
    {
        public OrderByProperty Select() => GetDefaultOrderByProperty(typeof(T));
    }
}