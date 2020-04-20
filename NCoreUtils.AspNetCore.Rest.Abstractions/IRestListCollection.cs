using System.Collections.Generic;
using System.Threading;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Defines functionality to implement REST LIST method for the concrete type.
    /// </summary>
    /// <typeparam name="T">Type of the target object.</typeparam>
    public interface IRestListCollection<T>
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
    }
}