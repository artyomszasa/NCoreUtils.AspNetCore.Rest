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
        private readonly ExpressionParser _expressionParser;

        private readonly HttpRestClient _httpRestClient;

        public DefaultRestClient(ExpressionParser expressionParser, HttpRestClient httpRestClient)
        {
            _expressionParser = expressionParser ?? throw new ArgumentNullException(nameof(expressionParser));
            _httpRestClient = httpRestClient ?? throw new ArgumentNullException(nameof(httpRestClient));
        }

        public IQueryable<T> Collection<T>()
            => DirectQuery.Create<T>(
                new QueryProvider(_expressionParser, new RestQueryExecutor(_httpRestClient))
            );

        public Task<TId> CreateAsync<TData, TId>(TData data, CancellationToken cancellationToken = default)
            where TData : IHasId<TId>
            => _httpRestClient.CreateAsync<TData, TId>(data, default, cancellationToken);

        public Task DeleteAsync<TData, TId>(TId id, CancellationToken cancellationToken = default)
            where TData : IHasId<TId>
            => _httpRestClient.DeleteAsync<TData, TId>(id, cancellationToken);

        public Task<TData> ItemAsync<TData, TId>(TId id, CancellationToken cancellationToken = default) where TData : IHasId<TId>
            => _httpRestClient.ItemAsync<TData, TId>(id, default, cancellationToken);

        public Task UpdateAsync<TData, TId>(TId id, TData data, CancellationToken cancellationToken = default) where TData : IHasId<TId>
            => _httpRestClient.UpdateAsync<TData, TId>(id, data, default, cancellationToken);
    }
}