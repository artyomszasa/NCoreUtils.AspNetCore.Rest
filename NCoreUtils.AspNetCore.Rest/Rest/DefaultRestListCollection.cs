using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data;
using NCoreUtils.Linq;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Provides default implementation for REST LIST method.
    /// </summary>
    /// <typeparam name="T">Type of the collection elements.</typeparam>
    public class DefaultRestListCollection<T> : IRestListCollection<T>
    {
        protected IDataRepository<T> Repository { get; }

        protected IRestQueryFilter<T> QueryFilter { get; }

        protected IRestQueryOrderer<T> QueryOrderer { get; }

        public DefaultRestListCollection(
            IServiceProvider serviceProvider,
            IDataRepository<T> repository,
            IRestQueryFilter<T>? queryFilter = null,
            IRestQueryOrderer<T>? queryOrderer = null)
        {
            if (serviceProvider is null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            QueryFilter = queryFilter ?? ActivatorUtilities.CreateInstance<DefaultQueryFilter<T>>(serviceProvider);
            QueryOrderer = queryOrderer ?? ActivatorUtilities.CreateInstance<DefaultQueryOrderer<T>>(serviceProvider);
        }

        /// <summary>
        /// Performes REST LIST action for the specified type with the specified parameters.
        /// </summary>
        /// <param name="restQuery">Query options specified in the request.</param>
        /// <param name="accessValidator">
        /// Queryable decorator that filters out non-accessible entities depending on the access configuration.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// REST LIST response that contains partial resultset defined by Filter, Offset and Count properties of the
        /// rest query parameter.
        /// </returns>
        public IAsyncEnumerable<T> InvokeAsync(RestQuery restQuery, AsyncQueryFilter accessValidator, CancellationToken cancellationToken)
        {
            var filteredQueryTask = Repository.Items
                // apply filters
                .Apply(QueryFilter, restQuery)
                // apply access limitations
                .ApplyAsync(accessValidator, cancellationToken);
            if (filteredQueryTask.IsCompletedSuccessfully)
            {
                var finalQuery = filteredQueryTask.Result
                    .Apply(QueryOrderer, restQuery)
                    .Skip(restQuery.Offset)
                    .Take(restQuery.Count);
                return finalQuery is IAsyncEnumerable<T> asEnumerable ? asEnumerable : finalQuery.ExecuteAsync(cancellationToken);
            }

            return AsyncEnumerable.Delay(async (ctoken) =>
            {
                var sourceQuery = await filteredQueryTask;
                var finalQuery = sourceQuery
                    .Apply(QueryOrderer, restQuery)
                    .Skip(restQuery.Offset)
                    .Take(restQuery.Count);
                return finalQuery is IAsyncEnumerable<T> asEnumerable ? asEnumerable : finalQuery.ExecuteAsync(ctoken);
            });
        }
    }
}