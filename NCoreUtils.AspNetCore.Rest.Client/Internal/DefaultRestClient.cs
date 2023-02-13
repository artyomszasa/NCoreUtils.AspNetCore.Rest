using System;
using System.Diagnostics.CodeAnalysis;
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
        protected IDataUtils DataUtils { get; }

        protected ExpressionParser ExpressionParser { get; }

        protected IHttpRestClient HttpRestClient { get; }

        public DefaultRestClient(
            IDataUtils dataUtils,
            ExpressionParser expressionParser,
            IHttpRestClient httpRestClient)
        {
            DataUtils = dataUtils ?? throw new ArgumentNullException(nameof(dataUtils));
            ExpressionParser = expressionParser ?? throw new ArgumentNullException(nameof(expressionParser));
            HttpRestClient = httpRestClient ?? throw new ArgumentNullException(nameof(httpRestClient));
        }

        public virtual IQueryable<T> Collection<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
            => DirectQuery.Create<T>(
                new QueryProvider(DataUtils, ExpressionParser, new RestQueryExecutor(HttpRestClient))
            );

        public virtual Task<TId> CreateAsync<TData, TId>(TData data, CancellationToken cancellationToken = default)
            where TData : IHasId<TId>
            => HttpRestClient.CreateAsync<TData, TId>(data, cancellationToken);

        public virtual Task DeleteAsync<TData, TId>(TId id, bool force, CancellationToken cancellationToken = default)
            where TData : IHasId<TId>
            => HttpRestClient.DeleteAsync<TData, TId>(id, force, cancellationToken);

        public virtual Task<TData?> ItemAsync<TData, TId>(TId id, CancellationToken cancellationToken = default) where TData : IHasId<TId>
            => HttpRestClient.ItemAsync<TData, TId>(id, cancellationToken);

        public virtual Task UpdateAsync<TData, TId>(TId id, TData data, CancellationToken cancellationToken = default) where TData : IHasId<TId>
            => HttpRestClient.UpdateAsync<TData, TId>(id, data, cancellationToken);
    }
}