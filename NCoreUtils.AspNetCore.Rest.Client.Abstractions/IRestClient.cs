using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data.Protocol;

namespace NCoreUtils.Rest;

public interface IRestClient
{
    Type DataType { get; }

    Type IdType { get; }
}

public interface IRestClient<TData> : IRestClient
{
    Type IRestClient.DataType => typeof(TData);

    IQueryable<TData> CreateQueryable();

    IAsyncEnumerable<TData> ListCollectionAsync(
        string? target = default,
        string? filter = default,
        string? sortBy = default,
        string? sortByDirection = default,
        IReadOnlyList<ThenBySorting>? thenBy = default,
        IReadOnlyList<string>? fields = default,
        IReadOnlyList<string>? includes = default,
        int offset = 0,
        int? limit = default,
        CancellationToken cancellationToken = default
    );

    Task<ReductionResult<TData>> ReductionAsync(
        Reduction reduction,
        string? target = null,
        string? filter = null,
        string? sortBy = null,
        string? sortByDirection = null,
        IReadOnlyList<ThenBySorting>? thenBy = default,
        int offset = 0,
        int? limit = null,
        CancellationToken cancellationToken = default
    );
}

public interface IRestClient<TData, TId> : IRestClient<TData>
{
    Task<TData?> ItemAsync(TId id, CancellationToken cancellationToken = default);

    Task<TId> CreateAsync(TData data, CancellationToken cancellationToken = default);

    Task UpdateAsync(TId id, TData data, CancellationToken cancellationToken = default);

    Task DeleteAsync(TId id, bool force, CancellationToken cancellationToken = default);
}