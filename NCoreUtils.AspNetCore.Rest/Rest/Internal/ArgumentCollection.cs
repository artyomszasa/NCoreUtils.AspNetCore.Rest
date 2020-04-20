using System;
using System.Collections;
using System.Collections.Generic;

namespace NCoreUtils.AspNetCore.Rest.Internal
{
    public struct ArgumentCollection<T> : IReadOnlyList<object>
    {
        public T Arg { get; }

        public int Count => 1;

        public object this[int index] => index switch
        {
            0 => Arg!,
            _ => throw new IndexOutOfRangeException()
        };

        public ArgumentCollection(T arg)
            => Arg = arg;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<object> GetEnumerator()
        {
            yield return Arg!;
        }
    }

    public struct ArgumentCollection<T1, T2> : IReadOnlyList<object>
    {
        public T1 Arg1 { get; }

        public T2 Arg2 { get; }

        public int Count => 2;

        public object this[int index] => index switch
        {
            0 => Arg1!,
            1 => Arg2!,
            _ => throw new IndexOutOfRangeException()
        };

        public ArgumentCollection(T1 arg1, T2 arg2)
        {
            Arg1 = arg1;
            Arg2 = arg2;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<object> GetEnumerator()
        {
            yield return Arg1!;
            yield return Arg2!;
        }
    }

    public struct ArgumentCollection<T1, T2, T3> : IReadOnlyList<object>
    {
        public T1 Arg1 { get; }

        public T2 Arg2 { get; }

        public T3 Arg3 { get; }

        public int Count => 3;

        public object this[int index] => index switch
        {
            0 => Arg1!,
            1 => Arg2!,
            2 => Arg2!,
            _ => throw new IndexOutOfRangeException()
        };

        public ArgumentCollection(T1 arg1, T2 arg2, T3 arg3)
        {
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<object> GetEnumerator()
        {
            yield return Arg1!;
            yield return Arg2!;
            yield return Arg3!;
        }
    }
}