using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data;
using NCoreUtils.Linq;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Provides default implementation for REST ITEM method.
    /// </summary>
    /// <typeparam name="TData">Type of the target object.</typeparam>
    /// <typeparam name="TId">Type of the Id property of the target object.</typeparam>
    public class DefaultRestItem<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>
        : IRestItem<TData, TId>, IBoxedInvoke<TId, AsyncQueryFilter, TData>
        where TData : IHasId<TId>
    {
        object IBoxedInvoke.Instance => this;

        /// Gets underlying data repository.
        protected IDataRepository<TData, TId> Repository { get; }

        /// <summary>
        /// Initializes new instance from the specified parameters.
        /// </summary>
        /// <param name="repository">Repository to use.</param>
        public DefaultRestItem(IDataRepository<TData, TId> repository)
            => Repository = repository ?? throw new ArgumentNullException(nameof(repository));

        /// <summary>
        /// Performes REST ITEM action for the predefined type.
        /// </summary>
        /// <param name="id">Id of the object to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// Object of the specified type for the specified id.
        /// </returns>
        public async ValueTask<TData> InvokeAsync(
            TId id,
            AsyncQueryFilter accessValidator,
            CancellationToken cancellationToken)
        {
            var query = Repository.Items.Where(QueryableExtensions.CreateIdEqualityPredicate<TData, TId>(id));
            var accessibleQuery = (IQueryable<TData>)await accessValidator(query, cancellationToken);
            var item = await accessibleQuery.FirstOrDefaultAsync(cancellationToken);
            if (item is null)
            {
                throw new NotFoundException();
            }
            return item;
        }
    }
}