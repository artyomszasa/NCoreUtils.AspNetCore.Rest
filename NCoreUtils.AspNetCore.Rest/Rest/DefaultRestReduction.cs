using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data;
using NCoreUtils.Linq;

namespace NCoreUtils.AspNetCore.Rest
{
    public class DefaultRestReduction<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : IRestReduction<T>
        where T : class
    {
        protected IDataRepository<T> Repository { get; }

        protected IRestQueryFilter<T> QueryFilter { get; }

        protected IRestQueryOrderer<T> QueryOrderer { get; }

        public DefaultRestReduction(
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

        protected virtual ValueTask<T?> ExecuteFirstOrDefaultAsync(IQueryable<T> queryable, CancellationToken cancellationToken = default)
            => new ValueTask<T?>(queryable.FirstOrDefaultAsync(cancellationToken)!);

        protected virtual ValueTask<T?> ExecuteSingleOrDefaultAsync(IQueryable<T> queryable, CancellationToken cancellationToken = default)
            => new ValueTask<T?>(queryable.SingleOrDefaultAsync(cancellationToken)!);

        protected virtual ValueTask<int> ExecuteCountAsync(IQueryable<T> queryable, CancellationToken cancellationToken = default)
            => new ValueTask<int>(queryable.CountAsync(cancellationToken)!);


        public async ValueTask<object?> InvokeAsync(RestQuery restQuery, string reduction, AsyncQueryFilter accessValidator, CancellationToken cancellationToken)
        {
            if (reduction is null)
            {
                throw new ArgumentNullException(nameof(reduction));
            }
            var query = await Repository.Items
                // apply filters
                .Apply(QueryFilter, restQuery)
                // apply access limitations
                .ApplyAsync(accessValidator, cancellationToken)
                .ConfigureAwait(false);
            var orderedQuery = query.Apply(QueryOrderer, restQuery);
            return reduction switch
            {
                DefaultReductions.First => await ExecuteFirstOrDefaultAsync(orderedQuery, cancellationToken),
                DefaultReductions.Single => await ExecuteSingleOrDefaultAsync(orderedQuery, cancellationToken),
                DefaultReductions.Count => await ExecuteCountAsync(orderedQuery, cancellationToken),
                _ => throw new NotSupportedException($"Reduction {reduction} is not supported")
            };
        }
    }
}