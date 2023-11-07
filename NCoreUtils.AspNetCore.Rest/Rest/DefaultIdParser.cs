using System;

namespace NCoreUtils.AspNetCore.Rest;

public sealed class DefaultIdParser : IIdParser
{
    private static DefaultIdParser? _singleton;

    public static DefaultIdParser Singleton => _singleton ??= new();

    public object? ParseId(string? raw, Type type)
    {
        if (raw is null)
        {
            return default;
        }
        if (type == typeof(Guid))
        {
            return Guid.Parse(raw);
        }
        return Convert.ChangeType(raw, type);
    }
}