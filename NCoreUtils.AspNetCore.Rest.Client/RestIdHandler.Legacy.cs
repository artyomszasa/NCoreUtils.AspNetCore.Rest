#if !NET7_0_OR_GREATER

using System;
using System.Globalization;

namespace NCoreUtils.Rest;

internal abstract class RestIdHandler<TId> : IRestIdHandler<TId>
    where TId : ISpanFormattable
{
    public abstract TId ParseId(ReadOnlySpan<char> input);

    public virtual bool TryStringifyId(TId id, Span<char> buffer, out int used)
        => id.TryFormat(buffer, out used, default, CultureInfo.InvariantCulture);
}

internal sealed class RestInt16IdHandler : RestIdHandler<short>
{
    public override short ParseId(ReadOnlySpan<char> input)
        => short.Parse(input, NumberStyles.Integer, CultureInfo.InvariantCulture);
}

internal sealed class RestInt32IdHandler : RestIdHandler<int>
{
    public override int ParseId(ReadOnlySpan<char> input)
        => int.Parse(input, NumberStyles.Integer, CultureInfo.InvariantCulture);
}

internal sealed class RestInt64IdHandler : RestIdHandler<long>
{
    public override long ParseId(ReadOnlySpan<char> input)
        => long.Parse(input, NumberStyles.Integer, CultureInfo.InvariantCulture);
}

internal sealed class RestGuidIdHandler : RestIdHandler<Guid>
{
    public override Guid ParseId(ReadOnlySpan<char> input)
        => Guid.Parse(input);
}

public static partial class RestIdHandler
{
    public static IRestIdHandler<short> Int16 { get; } = new RestInt16IdHandler();

    public static IRestIdHandler<int> Int32 { get; } = new RestInt32IdHandler();

    public static IRestIdHandler<long> Int64 { get; } = new RestInt64IdHandler();

    public static IRestIdHandler<Guid> Guid { get; } = new RestGuidIdHandler();
}

#endif