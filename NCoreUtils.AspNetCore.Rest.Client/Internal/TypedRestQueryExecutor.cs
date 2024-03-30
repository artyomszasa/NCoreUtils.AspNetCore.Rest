using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data.Protocol.Ast;
using NCoreUtils.Data.Protocol.Linq;

namespace NCoreUtils.Rest.Internal;

public class TypedRestQueryExecutor(RestClientContextFactory clientContextFactory) : IRestDataQueryExecutor
{
    protected RestClientContextFactory ClientContextFactory { get; } = clientContextFactory ?? throw new ArgumentNullException(nameof(clientContextFactory));

    public IAsyncEnumerable<T> ExecuteEnumerationAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        string target,
        Node? filter = null,
        Node? sortBy = null,
        bool isDescending = false,
        IReadOnlyList<string>? fields = null,
        IReadOnlyList<string>? includes = null,
        int offset = 0,
        int? limit = null)
    {
        var client = (TypedRestClient<T>)ClientContextFactory.CreateClient(typeof(T));
        return new DelayedAsyncEnumerable<T>(cancellationToken => new(client.ListCollectionAsync(
            target: target,
            filter: filter?.ToString(),
            sortBy: sortBy?.ToString(),
            sortByDirection: isDescending ? "desc" : "asc",
            fields: fields,
            includes: includes,
            offset: offset,
            limit: limit,
            cancellationToken: cancellationToken
        )));
    }

    public async Task<TResult> ExecuteReductionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TSource, TResult>(
        string target,
        string reduction,
        Node? filter = null,
        Node? sortBy = null,
        bool isDescending = false,
        int offset = 0,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var client = (TypedRestClient<TSource>)ClientContextFactory.CreateClient(typeof(TSource));
        var res = await client.ReductionAsync(
            reduction,
            target,
            filter?.ToString(),
            sortBy?.ToString(),
            sortByDirection: isDescending ? "desc" : "asc",
            offset: offset,
            limit,
            cancellationToken
        );
        // FIXME: should use ReductionResult in Protocol (?)
        if (typeof(TResult) == typeof(int))
        {
            return ReBox<TResult>(res.Int32Value);
        }
        if (typeof(TResult) == typeof(bool))
        {
            return ReBox<TResult>(res.BooleanValue);
        }
        if (typeof(TResult) == typeof(TSource))
        {
            return ReBox<TResult>(res.ItemValue!);
        }
        if (typeof(TResult) == typeof(long))
        {
            return ReBox<TResult>(res.Int64Value);
        }
        throw new NotSupportedException($"Reduction return type {typeof(TResult)} is not supported.");
    }

    // TODO: find better solution not involving casing...
    private static T ReBox<T>(object source)
        => (T)source;
}