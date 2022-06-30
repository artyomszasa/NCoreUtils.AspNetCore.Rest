using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest.Internal;

/// <summary>
/// Used to avoid reference to System.Linq.Async
/// </summary>
internal static class AsyncEnumerableExtensions
{
    public static async ValueTask<List<T>> ToListAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        var results = new List<T>();
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            results.Add(item);
        }
        return results;
    }
}