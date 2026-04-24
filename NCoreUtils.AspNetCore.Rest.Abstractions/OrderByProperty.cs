using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NCoreUtils.AspNetCore.Rest;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
[method: DebuggerStepThrough]
public readonly struct OrderByProperty(PropertyInfo property, bool isDescending) : IEquatable<OrderByProperty>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static bool operator==(OrderByProperty a, OrderByProperty b) => a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static bool operator!=(OrderByProperty a, OrderByProperty b) => !a.Equals(b);

    public static readonly OrderByProperty Default = default!;

    public PropertyInfo? Property { get; } = property;

    public bool IsDescending { get; } = isDescending;

    [MemberNotNullWhen(true, nameof(Property))]
    public bool HasValue => Property is not null;

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