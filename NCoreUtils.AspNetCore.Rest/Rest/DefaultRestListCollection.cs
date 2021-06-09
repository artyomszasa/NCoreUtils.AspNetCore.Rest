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

        /// <inheritdoc />
        public IAsyncEnumerable<T> InvokeAsync(
            RestQuery restQuery,
            AsyncQueryFilter accessValidator,
            CancellationToken cancellationToken)
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
                    .Skip(restQuery.GetOffset())
                    .TakeWhenNonNegative(restQuery.GetCount());
                return finalQuery is IAsyncEnumerable<T> asEnumerable ? asEnumerable : finalQuery.ExecuteAsync(cancellationToken);
            }

            return AsyncEnumerable.Delay(async (ctoken) =>
            {
                var sourceQuery = await filteredQueryTask;
                var finalQuery = sourceQuery
                    .Apply(QueryOrderer, restQuery)
                    .Skip(restQuery.GetOffset())
                    .TakeWhenNonNegative(restQuery.GetCount());
                return finalQuery is IAsyncEnumerable<T> asEnumerable ? asEnumerable : finalQuery.ExecuteAsync(ctoken);
            });
        }

        /// <inheritdoc />
        public IAsyncEnumerable<TPartial> InvokePartialAsync<TPartial>(
            RestQuery restQuery,
            AsyncQueryFilter accessValidator,
            System.Linq.Expressions.Expression<Func<T, TPartial>> selector,
            CancellationToken cancellationToken)
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
                    .Select(selector)
                    .Skip(restQuery.GetOffset())
                    .TakeWhenNonNegative(restQuery.GetCount());
                return finalQuery is IAsyncEnumerable<TPartial> asEnumerable ? asEnumerable : finalQuery.ExecuteAsync(cancellationToken);
            }

            return AsyncEnumerable.Delay(async (ctoken) =>
            {
                var sourceQuery = await filteredQueryTask;
                var finalQuery = sourceQuery
                    .Apply(QueryOrderer, restQuery)
                    .Select(selector)
                    .Skip(restQuery.GetOffset())
                    .TakeWhenNonNegative(restQuery.GetCount());
                return finalQuery is IAsyncEnumerable<TPartial> asEnumerable ? asEnumerable : finalQuery.ExecuteAsync(ctoken);
            });
        }
    }
}