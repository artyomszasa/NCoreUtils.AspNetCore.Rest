using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Defines functionality to implement REST ITEM method for concrete type.
    /// </summary>
    /// <typeparam name="TData">Type of the target object.</typeparam>
    /// <typeparam name="TId">Type of the Id property of the target object.</typeparam>
    public interface IRestItem<TData, TId>
        where TData : IHasId<TId>
    {
        /// <summary>
        /// Performes REST ITEM action for the predefined type.
        /// </summary>
        /// <param name="id">Id of the object to return.</param>
        /// <param name="accessValidator">
        /// Queryable decorator that filters out non-accessible entities depending on the access configuration.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// Object of the specified type for the specified id.
        /// </returns>
        ValueTask<TData> InvokeAsync(
            TId id,
            AsyncQueryFilter accessValidator,
            CancellationToken cancellationToken);
    }
}