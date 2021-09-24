using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data;
using NCoreUtils.Data.Protocol;
using NCoreUtils.Data.Protocol.Linq;

namespace NCoreUtils.Rest.Internal
{
    public class DefaultRestClient : IRestClient
    {
        protected ExpressionParser ExpressionParser { get; }

        protected IHttpRestClient HttpRestClient { get; }

        public DefaultRestClient(ExpressionParser expressionParser, IHttpRestClient httpRestClient)
        {
            ExpressionParser = expressionParser ?? throw new ArgumentNullException(nameof(expressionParser));
            HttpRestClient = httpRestClient ?? throw new ArgumentNullException(nameof(httpRestClient));
        }

        public virtual IQueryable<T> Collection<T>()
            => DirectQuery.Create<T>(
                new QueryProvider(ExpressionParser, new RestQueryExecutor(HttpRestClient))
            );

        public virtual Task<TId> CreateAsync<TData, TId>(TData data, CancellationToken cancellationToken = default)
            where TData : IHasId<TId>
            => HttpRestClient.CreateAsync<TData, TId>(data, cancellationToken);

        public virtual Task DeleteAsync<TData, TId>(TId id, bool force, CancellationToken cancellationToken = default)
            where TData : IHasId<TId>
            => HttpRestClient.DeleteAsync<TData, TId>(id, force, cancellationToken);

#if NETSTANDARD2_0
        public Task DeleteAsync<TData, TId>(TId id, CancellationToken cancellationToken = default)
            where TData : IHasId<TId>
            => DeleteAsync<TData, TId>(id, false, cancellationToken);
#endif


        public virtual Task<TData> ItemAsync<TData, TId>(TId id, CancellationToken cancellationToken = default) where TData : IHasId<TId>
            => HttpRestClient.ItemAsync<TData, TId>(id, cancellationToken);

        public virtual Task UpdateAsync<TData, TId>(TId id, TData data, CancellationToken cancellationToken = default) where TData : IHasId<TId>
            => HttpRestClient.UpdateAsync<TData, TId>(id, data, cancellationToken);
    }
}