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

public readonly struct ArgumentCollection<T>(T arg) : IReadOnlyList<object>
{
    public T Arg { get; } = arg;

    public int Count => 1;

    public object this[int index] => index switch
    {
        0 => Arg!,
        _ => throw new IndexOutOfRangeException()
    };

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<object> GetEnumerator()
    {
        yield return Arg!;
    }
}

public readonly struct ArgumentCollection<T1, T2>(T1 arg1, T2 arg2) : IReadOnlyList<object>
{
    public T1 Arg1 { get; } = arg1;

    public T2 Arg2 { get; } = arg2;

    public int Count => 2;

    public object this[int index] => index switch
    {
        0 => Arg1!,
        1 => Arg2!,
        _ => throw new IndexOutOfRangeException()
    };

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<object> GetEnumerator()
    {
        yield return Arg1!;
        yield return Arg2!;
    }
}

public readonly struct ArgumentCollection<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3) : IReadOnlyList<object>
{
    public T1 Arg1 { get; } = arg1;

    public T2 Arg2 { get; } = arg2;

    public T3 Arg3 { get; } = arg3;

    public int Count => 3;

    public object this[int index] => index switch
    {
        0 => Arg1!,
        1 => Arg2!,
        2 => Arg2!,
        _ => throw new IndexOutOfRangeException()
    };

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<object> GetEnumerator()
    {
        yield return Arg1!;
        yield return Arg2!;
        yield return Arg3!;
    }
}