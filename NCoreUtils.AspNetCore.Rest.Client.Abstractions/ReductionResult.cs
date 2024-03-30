using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NCoreUtils.Rest;

public interface IReductionResultVisitor<TData, TResult>
{
    TResult VisitNull();

    TResult VisitInt32(int value);

    TResult VisitInt64(long value);

    TResult VisitBoolean(bool value);

    TResult VisitItem(TData value);
}

public readonly struct ReductionResult<T> : IEquatable<ReductionResult<T>>
{
    [StructLayout(LayoutKind.Explicit)]
    private readonly struct DataUnion
    {
        [FieldOffset(0)]
        public readonly int Int32Value;

        [FieldOffset(0)]
        public readonly long Int64Value;

        [FieldOffset(0)]
        public readonly bool BooleanValue;

        [FieldOffset(0)]
        public readonly T ItemValue;

#pragma warning disable CS8618
        public DataUnion(int value) => Int32Value = value;

        public DataUnion(long value) => Int64Value = value;

        public DataUnion(bool value) => BooleanValue = value;

        public DataUnion(T value) => ItemValue = value;
#pragma warning restore CS8618
    }

    private enum Tag
    {
        Null = 0,
        Int32,
        Int64,
        Boolean,
        Item
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator==(ReductionResult<T> a, ReductionResult<T> b)
        => a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator!=(ReductionResult<T> a, ReductionResult<T> b)
        => !a.Equals(b);

    public static ReductionResult<T> Null
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReductionResult<T> Int32(int value)
        => new(Tag.Int32, new(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReductionResult<T> Int64(long value)
        => new(Tag.Int64, new(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReductionResult<T> Boolean(bool value)
        => new(Tag.Boolean, new(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReductionResult<T> Item(T value)
        => new(Tag.Item, new(value ?? throw new ArgumentNullException(nameof(value))));

    private readonly Tag _tag;

    private readonly DataUnion _data;

    public bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _tag == Tag.Null;
    }

    public bool IsInt32
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _tag == Tag.Int32;
    }

    public bool IsInt64
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _tag == Tag.Int64;
    }

    public bool IsBoolean
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _tag == Tag.Boolean;
    }

    [MemberNotNullWhen(true, nameof(ItemValue))]
    public bool IsItem
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _tag == Tag.Item;
    }

    public int Int32Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsInt32 ? _data.Int32Value : throw new InvalidCastException("Reduction result is not an int.");
    }

    public long Int64Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsInt64 ? _data.Int64Value : throw new InvalidCastException("Reduction result is not a long.");
    }

    public bool BooleanValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsBoolean ? _data.BooleanValue : throw new InvalidCastException("Reduction result is not a bool.");
    }

    public T ItemValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsItem ? _data.ItemValue : throw new InvalidCastException("Reduction result is not an item.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReductionResult(Tag tag, DataUnion data)
    {
        _tag = tag;
        _data = data;
    }

    public TResult Accept<TResult>(IReductionResultVisitor<T, TResult> visitor) => _tag switch
    {
        Tag.Null => visitor.VisitNull(),
        Tag.Int32 => visitor.VisitInt32(_data.Int32Value),
        Tag.Int64 => visitor.VisitInt64(_data.Int64Value),
        Tag.Boolean => visitor.VisitBoolean(_data.BooleanValue),
        Tag.Item => visitor.VisitItem(_data.ItemValue),
        _ => throw new InvalidOperationException("Should never happen.")
    };

    public bool Equals(ReductionResult<T> other) => _tag switch
    {
        Tag.Null => other.IsNull,
        Tag.Int32 => other._tag == Tag.Int32 && _data.Int32Value == other._data.Int32Value,
        Tag.Int64 => other._tag == Tag.Int64 && _data.Int64Value == other._data.Int64Value,
        Tag.Boolean => other._tag == Tag.Boolean && _data.BooleanValue == other._data.BooleanValue,
        Tag.Item => other._tag == Tag.Item && ReferenceEquals(_data.ItemValue, other._data.ItemValue),
        _ => throw new InvalidOperationException("Should never happen.")
    };

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is ReductionResult<T> other && Equals(other);

    public override int GetHashCode() => _tag switch
    {
        Tag.Null => 0,
        Tag.Int32 => HashCode.Combine(Tag.Int32, _data.Int32Value),
        Tag.Int64 => HashCode.Combine(Tag.Int64, _data.Int64Value),
        Tag.Boolean => HashCode.Combine(Tag.Boolean, _data.BooleanValue),
        Tag.Item => HashCode.Combine(Tag.Item, RuntimeHelpers.GetHashCode(_data.ItemValue)),
        _ => throw new InvalidOperationException("Should never happen.")
    };

    public override string? ToString() => _tag switch
    {
        Tag.Null => null,
        Tag.Int32 => _data.Int32Value.ToString(),
        Tag.Int64 => _data.Int64Value.ToString(),
        Tag.Boolean => _data.BooleanValue.ToString(),
        Tag.Item => _data.ItemValue?.ToString(),
        _ => "<<invalid>>"
    };
}