using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Defines functionality to implement reduction extension for the concrete type.
    /// </summary>
    /// <typeparam name="T">Type of the target object.</typeparam>
    public interface IRestReduction<T>
    {
        /// <summary>
        /// Performes specified reduction for the predefined type with the specified parameters.
        /// </summary>
        /// <param name="restQuery">Query options specified in the request.</param>
        /// <param name="reduction">Reduction to perform.</param>
        /// <param name="accessValidator">
        /// Queryable decorator that filters out non-accessible entities depending on the access configuration.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// Reduction response that contains partial resultset defined by Offset and Count properties of the rest query
        /// parameter.
        /// </returns>
        ValueTask<object?> InvokeAsync(
            RestQuery restQuery,
            string reduction,
            AsyncQueryFilter accessValidator,
            CancellationToken cancellationToken);
    }
}