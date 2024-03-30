#if NET7_0_OR_GREATER

using System;
using System.Globalization;

namespace NCoreUtils.Rest;

internal sealed class RestIdHandler<TId> : IRestIdHandler<TId>
    where TId : ISpanFormattable, ISpanParsable<TId>
{
    public TId ParseId(ReadOnlySpan<char> input)
        => TId.Parse(input, CultureInfo.InvariantCulture);

    public bool TryStringifyId(TId id, Span<char> buffer, out int used)
        => id.TryFormat(buffer, out used, default, CultureInfo.InvariantCulture);
}

public static partial class RestIdHandler
{
    public static IRestIdHandler<short> Int16 { get; } = new RestIdHandler<short>();

    public static IRestIdHandler<int> Int32 { get; } = new RestIdHandler<int>();

    public static IRestIdHandler<long> Int64 { get; } = new RestIdHandler<long>();

    public static IRestIdHandler<Guid> Guid { get; } = new RestIdHandler<Guid>();
}

#endif