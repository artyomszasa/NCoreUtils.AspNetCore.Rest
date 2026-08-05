using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NCoreUtils.AspNetCore.Rest;

internal static class G
{
    public static ActivitySource ActivitySource { get; } = new ActivitySource(
        "NCoreUtils.AspNetCore.Rest",
        "8.0.0"
    );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ValueTask DisposeSynchronously(IDisposable disposable)
    {
        disposable.Dispose();
        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask DisposeAsync(object? maybeDisposable) => maybeDisposable switch
    {
        null => default,
        IAsyncDisposable asyncDisposable => asyncDisposable.DisposeAsync(),
        IDisposable disposable => DisposeSynchronously(disposable),
        _ => default
    };
}