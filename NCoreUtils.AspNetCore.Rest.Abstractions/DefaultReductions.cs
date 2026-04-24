using System.Collections.Generic;

#if !NET8_0_OR_GREATER
using System.Collections.Immutable;
#else
using System.Collections.Frozen;
#endif

namespace NCoreUtils.AspNetCore.Rest;

public static class DefaultReductions
{
    public const string First = "first";

#pragma warning disable CA1720 // Identifier contains type name
    public const string Single = "single";
#pragma warning restore CA1720 // Identifier contains type name

    public const string Count = "count";

    public const string Any = "any";

#if NET8_0_OR_GREATER
    public static FrozenSet<string> Names { get; } = new HashSet<string>
    {
        First,
        Single,
        Count,
        Any
    }.ToFrozenSet();
#else
    public static ImmutableHashSet<string> Names { get; } = ImmutableHashSet.CreateRange(new []
    {
        First,
        Single,
        Count,
        Any
    });
#endif
}