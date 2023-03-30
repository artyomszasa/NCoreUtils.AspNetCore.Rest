using System.Collections.Immutable;

namespace NCoreUtils.AspNetCore.Rest
{
    public static class DefaultReductions
    {
        public const string First = "first";

        public const string Single = "single";

        public const string Count = "count";

        public const string Any = "any";

        public static ImmutableHashSet<string> Names = ImmutableHashSet.CreateRange(new []
        {
            First,
            Single,
            Count,
            Any
        });
    }
}