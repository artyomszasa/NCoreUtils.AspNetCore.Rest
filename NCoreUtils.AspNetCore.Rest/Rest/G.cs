using System.Diagnostics;

namespace NCoreUtils.AspNetCore.Rest;

internal static class G
{
    public static ActivitySource ActivitySource { get; } = new ActivitySource(
        "NCoreUtils.AspNetCore.Rest",
        "8.0.0"
    );
}