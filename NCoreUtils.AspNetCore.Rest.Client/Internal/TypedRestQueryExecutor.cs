using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data.Protocol;
using NCoreUtils.Data.Protocol.Ast;
using NCoreUtils.Data.Protocol.Linq;

namespace NCoreUtils.Rest.Internal;

public class TypedRestQueryExecutor(IServiceProvider serviceProvider) : IRestDataQueryExecutor
{
    private static TypedRestClient<T> CastRestClient<T>(TypedRestClient client) => client switch
    {
        TypedRestClient<T> clientOfT => clientOfT,
        _ => throw new InvalidOperationException($"Rest client configuration returned client of invalid type: TypedRestClient<{typeof(T)}> expected, TypeRestClient<{client.DataType}> returned.")
    };

    private RestClientContextFactory? _clientContextFactory;

    private IServiceProvider ServiceProvider { get; } = serviceProvider;

    protected RestClientContextFactory ClientContextFactory => _clientContextFactory ??= ServiceProvider.GetRequiredService<RestClientContextFactory>();

    public IAsyncEnumerable<T> ExecuteEnumerationAsync<T>(
        string target,
        Node? filter = null,
        Node? sortBy = null,
        bool isDescending = false,
        IReadOnlyList<ThenByOrdering>? thenBy = default,
        IReadOnlyList<string>? fields = null,
        IReadOnlyList<string>? includes = null,
        int offset = 0,
        int? limit = null)
    {
        var client = CastRestClient<T>(ClientContextFactory.CreateClient(typeof(T)));
        return new DelayedAsyncEnumerable<T>(cancellationToken => new(client.ListCollectionAsync(
            target: target,
            filter: filter?.ToString(),
            sortBy: sortBy?.ToString(),
            sortByDirection: isDescending ? "desc" : "asc",
            thenBy: thenBy is null
                ? default
                : thenBy.MapToArray(ord => new ThenBySorting(ord.Expression?.ToString() ?? string.Empty, ord.IsDescending ? "desc" : "asc")),
            fields: fields,
            includes: includes,
            offset: offset,
            limit: limit,
            cancellationToken: cancellationToken
        )));
    }

    [Obsolete("Use variation that handles thenBy instead.")]
    public IAsyncEnumerable<T> ExecuteEnumerationAsync<T>(
        string target,
        Node? filter = null,
        Node? sortBy = null,
        bool isDescending = false,
        IReadOnlyList<string>? fields = null,
        IReadOnlyList<string>? includes = null,
        int offset = 0,
        int? limit = null)
        => ExecuteEnumerationAsync<T>(
            target,
            filter,
            sortBy,
            isDescending,
            null,
            fields,
            includes,
            offset,
            limit
        );

    public async Task<TResult> ExecuteReductionAsync<TSource, TResult>(
        string target,
        Reduction reduction,
        Node? filter = null,
        Node? sortBy = null,
        bool isDescending = false,
        IReadOnlyList<ThenByOrdering>? thenBy = default,
        int offset = 0,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var client = CastRestClient<TSource>(ClientContextFactory.CreateClient(typeof(TSource)));
        var res = await client.ReductionAsync(
            reduction,
            target,
            filter?.ToString(),
            sortBy?.ToString(),
            sortByDirection: isDescending ? "desc" : "asc",
            thenBy: thenBy is null
                ? default
                : thenBy.MapToArray(ord => new ThenBySorting(ord.Expression?.ToString() ?? string.Empty, ord.IsDescending ? "desc" : "asc")),
            offset: offset,
            limit,
            cancellationToken
        );
        return res.TryGetValue<TResult>(out var result)
            ? result!
            : throw new InvalidCastException($"{res} cannot be converted to {typeof(TResult)}");
    }

    [Obsolete("Use variation that handles thenBy instead.")]
    public Task<TResult> ExecuteReductionAsync<TSource, TResult>(
        string target,
        Reduction reduction,
        Node? filter = null,
        Node? sortBy = null,
        bool isDescending = false,
        int offset = 0,
        int? limit = null,
        CancellationToken cancellationToken = default)
        => ExecuteReductionAsync<TSource, TResult>(
            target,
            reduction,
            filter,
            sortBy,
            isDescending,
            default,
            offset,
            limit,
            cancellationToken
        );
}