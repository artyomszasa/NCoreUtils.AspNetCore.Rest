using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Defines functionality to implement REST LIST method for the concrete type.
    /// </summary>
    /// <typeparam name="T">Type of the target object.</typeparam>
    public interface IRestListCollection<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
    {
        /// <summary>
        /// Performes REST LIST action for the predefined type with the specified parameters.
        /// </summary>
        /// <param name="restQuery">Query options specified in the request.</param>
        /// <param name="accessValidator">
        /// Queryable decorator that filters out non-accessible entities depending on the access configuration.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// REST LIST response that contains partial resultset defined by Offset and Count properties of the rest query
        /// parameter.
        /// </returns>
        IAsyncEnumerable<T> InvokeAsync(
            RestQuery restQuery,
            AsyncQueryFilter accessValidator,
            CancellationToken cancellationToken);

        /// <summary>
        /// Performes REST LIST action for the partial type with the specified parameters.
        /// </summary>
        /// <param name="restQuery">Query options specified in the request.</param>
        /// <param name="accessValidator">
        /// Queryable decorator that filters out non-accessible entities depending on the access configuration.
        /// </param>
        /// <param name="selector">Selector used to extract partial data from the query items.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <typeparam name="TPartial">Type of the partial data.</typeparam>
        /// <returns>
        /// REST LIST response that contains partial resultset defined by Offset and Count properties of the rest query
        /// parameter.
        /// </returns>
        IAsyncEnumerable<TPartial> InvokePartialAsync<TPartial>(
            RestQuery restQuery,
            AsyncQueryFilter accessValidator,
            Expression<Func<T, TPartial>> selector,
            CancellationToken cancellationToken);
    }
}