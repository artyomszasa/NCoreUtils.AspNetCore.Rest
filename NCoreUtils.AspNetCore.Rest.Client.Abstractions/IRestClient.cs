using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Rest;

public interface IRestClient
{
    Type DataType { get; }

    Type IdType { get; }
}

public interface IRestClient<TData> : IRestClient
{
    Type IRestClient.DataType => typeof(TData);

    IAsyncEnumerable<TData> ListCollectionAsync(
        string? target = default,
        string? filter = default,
        string? sortBy = default,
        string? sortByDirection = default,
        IReadOnlyList<string>? fields = default,
        IReadOnlyList<string>? includes = default,
        int offset = 0,
        int? limit = default,
        CancellationToken cancellationToken = default
    );

    Task<ReductionResult<TData>> ReductionAsync(
        string reduction,
        string? target = null,
        string? filter = null,
        string? sortBy = null,
        string? sortByDirection = null,
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