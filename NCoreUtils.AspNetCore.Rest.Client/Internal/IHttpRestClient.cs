using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data;

namespace NCoreUtils.Rest.Internal
{
    public interface IHttpRestClient
    {
        Task<IReadOnlyList<T>> ListCollectionAsync<T>(
            string? target = default,
            string? filter = default,
            string? sortBy = default,
            string? sortByDirection = default,
            IReadOnlyList<string>? fields = default,
            IReadOnlyList<string>? includes = default,
            int offset = 0,
            int? limit = default,
            CancellationToken cancellationToken = default);

        Task<TData> ItemAsync<TData, TId>(
            TId id,
            CancellationToken cancellationToken = default)
            where TData : IHasId<TId>;

        Task<TId> CreateAsync<TData, TId>(
            TData data,
            CancellationToken cancellationToken = default)
            where TData : IHasId<TId>;

        Task<object> ReductionAsync<T>(
            string reduction,
            string? target = default,
            string? filter = default,
            string? sortBy = default,
            string? sortByDirection = default,
            int offset = 0,
            int? limit = default,
            CancellationToken cancellationToken = default);

        Task UpdateAsync<TData, TId>(
            TId id,
            TData data,
            CancellationToken cancellationToken = default)
            where TData : IHasId<TId>;

        Task DeleteAsync<TData, TId>(TId id, CancellationToken cancellationToken = default)
            where TData : IHasId<TId>;


    }
}