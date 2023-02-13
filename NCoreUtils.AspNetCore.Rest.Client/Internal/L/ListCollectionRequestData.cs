using System;
using System.Buffers;
using System.Collections.Generic;
using NCoreUtils.Memory;

namespace NCoreUtils.Rest.Internal.L
{
    public struct ListCollectionRequestData : ISpanExactEmplaceable
    {
        private const string LogPrefix = "Executing COLLECTION method ";

        public const int MaxStackAllocSize = 2048;

        public static Func<ListCollectionRequestData, Exception?, string> LogFormatter { get; }
            = (data, _) =>
            {
                var size = data.GetEmplaceBufferSize();
                size += LogPrefix.Length + 1;
                if (size > MaxStackAllocSize)
                {
                    var buffer = ArrayPool<char>.Shared.Rent(size);
                    try
                    {
                        var used = DoEmplace(buffer.AsSpan(), in data);
                        return new string(buffer, 0, used);
                    }
                    finally
                    {
                        ArrayPool<char>.Shared.Return(buffer);
                    }
                }
                Span<char> stackBuffer = stackalloc char[size];
                var stackUsed = DoEmplace(stackBuffer, in data);
                return stackBuffer.Slice(0, stackUsed).ToString();

                static int DoEmplace(Span<char> buffer, in ListCollectionRequestData data)
                {
                    var builder = new SpanBuilder(buffer);
                    builder.Append(LogPrefix);
                    builder.Append(data);
                    builder.Append('.');
                    return builder.Length;
                }
            };

        private static int GetStringifiedSize(int i)
        {
            int sign;
            if (i < 0)
            {
                i *= -1;
                sign = 1;
            }
            else
            {
                sign = 0;
            }
            if (i < 10) { return 1 + sign; }
            if (i < 100) { return 2 + sign; }
            if (i < 1_000) { return 3 + sign; }
            if (i < 10_000) { return 4 + sign; }
            if (i < 100_000) { return 5 + sign; }
            if (i < 1_000_000) { return 6 + sign; }
            if (i < 10_000_000) { return 7 + sign; }
            if (i < 100_000_000) { return 8 + sign; }
            if (i < 1_000_000_000) { return 9 + sign; }
            return 10 + sign;
        }

        public string? Target { get; }

        public string? Filter { get; }

        public string? SortBy { get; }

        public string? SortByDirection { get; }

        public IReadOnlyList<string>? Fields { get; }

        public IReadOnlyList<string>? Includes { get; }

        public int Offset { get; }

        public int? Limit { get; }

        public ListCollectionRequestData(
            string? target,
            string? filter,
            string? sortBy,
            string? sortByDirection,
            IReadOnlyList<string>? fields,
            IReadOnlyList<string>? includes,
            int offset,
            int? limit)
        {
            Target = target;
            Filter = filter;
            SortBy = sortBy;
            SortByDirection = sortByDirection;
            Fields = fields;
            Includes = includes;
            Offset = offset;
            Limit = limit;
        }

        public int GetEmplaceBufferSize()
        {
            var size = 2;
            var values = 0;
            if (!string.IsNullOrEmpty(Target))
            {
                ++values;
                size += 3 + nameof(Target).Length + Target!.Length;
            }
            if (!string.IsNullOrEmpty(Filter))
            {
                ++values;
                size += 3 + nameof(Filter).Length + Filter!.Length;
            }
            if (!string.IsNullOrEmpty(SortBy))
            {
                ++values;
                size += 3 + nameof(SortBy).Length + SortBy!.Length;
            }
            if (!string.IsNullOrEmpty(SortByDirection))
            {
                ++values;
                size += 3 + nameof(SortByDirection).Length + SortByDirection!.Length;
            }
            if (!(Fields is null || Fields.Count == 0))
            {
                ++values;
                size += 3 + nameof(Fields).Length;
                var items = 0;
                foreach (var item in Fields)
                {
                    ++items;
                    size += item.Length;
                }
                size += (items - 1) * 2;
            }
            if (!(Includes is null || Includes.Count == 0))
            {
                ++values;
                size += 3 + nameof(Includes).Length;
                var items = 0;
                foreach (var item in Includes)
                {
                    ++items;
                    size += item.Length;
                }
                size += (items - 1) * 2;
            }
            ++values;
            size += 3 + nameof(Offset).Length + GetStringifiedSize(Offset);
            if (Limit.HasValue)
            {
                ++values;
                size += 3 + nameof(Limit).Length + GetStringifiedSize(Limit.Value);
            }
            return size + ((values - 1) * 2);
        }

        public int Emplace(Span<char> span)
        {
            var size = GetEmplaceBufferSize();
            if (size > span.Length || !TryEmplace(span, out var used))
            {
                throw new InsufficientBufferSizeException(in span, size);
            }
            return used;
        }

        public bool TryEmplace(Span<char> span, out int used)
        {
            var builder = new SpanBuilder(span);
            var first = true;
            if (builder.TryAppend('[')
                && builder.TryAppendProperty(ref first, nameof(Target), Target)
                && builder.TryAppendProperty(ref first, nameof(Filter), Filter)
                && builder.TryAppendProperty(ref first, nameof(SortBy), SortBy)
                && builder.TryAppendProperty(ref first, nameof(SortByDirection), SortByDirection)
                && builder.TryAppendProperty(ref first, nameof(Fields), Fields)
                && builder.TryAppendProperty(ref first, nameof(Includes), Includes)
                && builder.TryAppendProperty(ref first, nameof(Offset), Offset)
                && builder.TryAppendProperty(ref first, nameof(Limit), Limit)
                && builder.TryAppend(']'))
            {
                used = builder.Length;
                return true;
            }
            used = default;
            return false;
        }

        public override string ToString()
            => this.ToStringUsingArrayPool();

        public string ToString(string? format, System.IFormatProvider? formatProvider)
            => ToString();

#if !NET6_0_OR_GREATER

        bool ISpanEmplaceable.TryGetEmplaceBufferSize(out int minimumBufferSize)
        {
            minimumBufferSize = GetEmplaceBufferSize();
            return true;
        }

        bool ISpanEmplaceable.TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format, System.IFormatProvider? provider)
            => TryEmplace(destination, out charsWritten);
#endif
    }
}