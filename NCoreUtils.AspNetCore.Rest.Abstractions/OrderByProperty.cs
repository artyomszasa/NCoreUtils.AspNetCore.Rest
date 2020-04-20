using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NCoreUtils.AspNetCore.Rest
{
    public struct OrderByProperty : IEquatable<OrderByProperty>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static bool operator==(OrderByProperty a, OrderByProperty b) => a.Equals(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static bool operator!=(OrderByProperty a, OrderByProperty b) => !a.Equals(b);

        public PropertyInfo Property { get; }

        public bool IsDescending { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public OrderByProperty(PropertyInfo property, bool isDescending)
        {
            Property = property;
            IsDescending = isDescending;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public bool Equals(OrderByProperty other)
            => Property == other.Property
                && IsDescending == other.IsDescending;

        [DebuggerStepThrough]
        public override bool Equals(object? obj)
            => obj is OrderByProperty other && Equals(other);

        [DebuggerStepThrough]
        public override int GetHashCode()
            => HashCode.Combine(Property, IsDescending);
    }
}