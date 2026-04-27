using System;

namespace NCoreUtils.Rest.Internal;

public sealed class DefaultRestTypeNameResolver : IRestTypeNameResolver
{
    public static DefaultRestTypeNameResolver Singleton { get; } = new();

    public string ResolveTypeName(Type type)
        => type.ThrowIfNull().Name;
}