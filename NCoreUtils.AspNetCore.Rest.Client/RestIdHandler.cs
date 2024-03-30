using System;
using System.Globalization;

namespace NCoreUtils.Rest;

public interface IRestIdHandler<TId>
{
    TId ParseId(ReadOnlySpan<char> input);

    bool TryStringifyId(TId id, Span<char> buffer, out int used);
}

internal sealed class RestStringIdHandler : IRestIdHandler<string>
{
    public string ParseId(ReadOnlySpan<char> input)
        => new(input);

    public bool TryStringifyId(string id, Span<char> buffer, out int used)
    {
        if (id.TryCopyTo(buffer))
        {
            used = id.Length;
            return true;
        }
        used = default;
        return false;
    }
}

public static partial class RestIdHandler
{
    private static T ReBox<T>(object obj)
        => (T)obj;

    public static IRestIdHandler<string> String { get; } = new RestStringIdHandler();

    public static IRestIdHandler<TId> For<TId>()
    {
        if (typeof(int) == typeof(TId))
        {
            return ReBox<IRestIdHandler<TId>>(Int32);
        }
        if (typeof(string) == typeof(TId))
        {
            return ReBox<IRestIdHandler<TId>>(String);
        }
        if (typeof(Guid) == typeof(TId))
        {
            return ReBox<IRestIdHandler<TId>>(Guid);
        }
        if (typeof(long) == typeof(TId))
        {
            return ReBox<IRestIdHandler<TId>>(Int64);
        }
        if (typeof(short) == typeof(TId))
        {
            return ReBox<IRestIdHandler<TId>>(Int16);
        }
        throw new InvalidOperationException($"No predefined id handler found for {typeof(TId)}, use custom handler instead.");
    }
}

/*
public abstract class RestIdHandler<TId>
    where TId : ISpanFormattable, IParsable<TId>
{
    public abstract TId ParseId(ReadOnlySpan<char> input);

    public abstract bool TryStringifyId(TId id, Span<char> buffer, out int used);
}
*/

