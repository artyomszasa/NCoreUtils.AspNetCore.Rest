using System;

namespace NCoreUtils.Rest.Internal;

public sealed class DefaultIdParser : IIdParser
{
    private static DefaultIdParser? _singleton;

    public static DefaultIdParser Singleton => _singleton ??= new();

    public object? ParseId(string? raw, Type idType)
    {
        if (raw is null)
        {
            return default;
        }
        if (idType == typeof(Guid))
        {
            return Guid.Parse(raw);
        }
        return Convert.ChangeType(raw, idType);
    }
}