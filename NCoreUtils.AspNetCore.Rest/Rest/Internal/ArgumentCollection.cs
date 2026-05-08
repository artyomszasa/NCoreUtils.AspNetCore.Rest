using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NCoreUtils.AspNetCore.Rest.Internal;

public static class ArgumentCollection
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static ArgumentCollection<T> Create<T>(T arg)
        => new(arg);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static ArgumentCollection<T1, T2> Create<T1, T2>(T1 arg1, T2 arg2)
        => new(arg1, arg2);
}

public readonly struct ArgumentCollection<T>(T arg) : IReadOnlyList<object>, IEquatable<ArgumentCollection<T>>
{
    public T Arg { get; } = arg;

    public int Count => 1;

    public object this[int index] => index switch
    {
        0 => Arg!,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<object> GetEnumerator()
    {
        yield return Arg!;
    }

    public override bool Equals(object? obj)
        => obj is ArgumentCollection<T> other && Equals(other);

    public bool Equals(ArgumentCollection<T> other)
        => EqualityComparer<T>.Default.Equals(Arg, other.Arg);

    public override int GetHashCode()
        => HashCode.Combine(Arg);

    public static bool operator ==(ArgumentCollection<T> left, ArgumentCollection<T> right)
        => left.Equals(right);

    public static bool operator !=(ArgumentCollection<T> left, ArgumentCollection<T> right)
        => !left.Equals(right);
}

public readonly struct ArgumentCollection<T1, T2>(T1 arg1, T2 arg2) : IReadOnlyList<object>, IEquatable<ArgumentCollection<T1, T2>>
{
    public T1 Arg1 { get; } = arg1;

    public T2 Arg2 { get; } = arg2;

    public int Count => 2;

    public object this[int index] => index switch
    {
        0 => Arg1!,
        1 => Arg2!,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<object> GetEnumerator()
    {
        yield return Arg1!;
        yield return Arg2!;
    }

    public override bool Equals(object? obj)
        => obj is ArgumentCollection<T1, T2> other && Equals(other);

    public bool Equals(ArgumentCollection<T1, T2> other)
        => EqualityComparer<T1>.Default.Equals(Arg1, other.Arg1)
            && EqualityComparer<T2>.Default.Equals(Arg2, other.Arg2);

    public override int GetHashCode()
        => HashCode.Combine(Arg1, Arg2);

    public static bool operator ==(ArgumentCollection<T1, T2> left, ArgumentCollection<T1, T2> right)
        => left.Equals(right);

    public static bool operator !=(ArgumentCollection<T1, T2> left, ArgumentCollection<T1, T2> right)
        => !left.Equals(right);
}

public readonly struct ArgumentCollection<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3) : IReadOnlyList<object>, IEquatable<ArgumentCollection<T1, T2, T3>>
{
    public T1 Arg1 { get; } = arg1;

    public T2 Arg2 { get; } = arg2;

    public T3 Arg3 { get; } = arg3;

    public int Count => 3;

    // FIXME: The position of 2 is returned as "Arg2". Why doesn't it return "Arg3"?
    public object this[int index] => index switch
    {
        0 => Arg1!,
        1 => Arg2!,
        2 => Arg2!,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<object> GetEnumerator()
    {
        yield return Arg1!;
        yield return Arg2!;
        yield return Arg3!;
    }

    public override bool Equals(object? obj)
        => obj is ArgumentCollection<T1, T2, T3> other && Equals(other);

    public bool Equals(ArgumentCollection<T1, T2, T3> other)
        => EqualityComparer<T1>.Default.Equals(Arg1, other.Arg1)
            && EqualityComparer<T2>.Default.Equals(Arg2, other.Arg2)
            && EqualityComparer<T3>.Default.Equals(Arg3, other.Arg3);

    public override int GetHashCode()
        => HashCode.Combine(Arg1, Arg2, Arg3);

    public static bool operator ==(ArgumentCollection<T1, T2, T3> left, ArgumentCollection<T1, T2, T3> right)
        => left.Equals(right);

    public static bool operator !=(ArgumentCollection<T1, T2, T3> left, ArgumentCollection<T1, T2, T3> right)
        => !left.Equals(right);
}