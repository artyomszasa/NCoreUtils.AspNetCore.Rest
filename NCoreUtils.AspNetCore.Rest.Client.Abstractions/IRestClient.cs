using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data;

namespace NCoreUtils.Rest
{
    public interface IRestClient
    {
        IQueryable<T> Collection<T>();

        Task<TData> ItemAsync<TData, TId>(TId id, CancellationToken cancellationToken = default)
            where TData : IHasId<TId>;

        Task<TId> CreateAsync<TData, TId>(TData data, CancellationToken cancellationToken = default)
            where TData : IHasId<TId>;

        Task UpdateAsync<TData, TId>(TId id, TData data, CancellationToken cancellationToken = default)
            where TData : IHasId<TId>;

        Task DeleteAsync<TData, TId>(TId id, bool force, CancellationToken cancellationToken = default)
            where TData : IHasId<TId>;

        [Obsolete("Use DeleteAsync(id, force, cancellationToken) instead.")]
        Task DeleteAsync<TData, TId>(TId id, CancellationToken cancellationToken = default)
            where TData : IHasId<TId>
#if NETSTANDARD2_0
            ;
#else
            => DeleteAsync<TData, TId>(id, false, cancellationToken);
#endif
    }
}