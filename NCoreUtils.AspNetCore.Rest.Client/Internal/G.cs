using System.Diagnostics;

namespace NCoreUtils.Rest.Internal;

internal static class G
{
    public static ActivitySource ActivitySource { get; } = new ActivitySource(
        "NCoreUtils.AspNetCore.Rest.Client",
        "8.0.0"
    );
}