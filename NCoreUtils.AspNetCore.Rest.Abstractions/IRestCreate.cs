using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Defines functionality to implement REST CREATE method for the concrete type.
    /// </summary>
    /// <typeparam name="TData">Type of the target object.</typeparam>
    /// <typeparam name="TId">Type of the Id property of the target object.</typeparam>
    public interface IRestCreate<TData, TId> : IRestTransactedMethod
        where TData : IHasId<TId>
    {
        /// <summary>
        /// Performes REST CREATE action for the predefined type.
        /// </summary>
        /// <param name="data">Object to insert into dataset.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// Object returned by dataset after insert operation. Depending on the dataset implementation some of the values of
        /// the returned object may differ from the input.
        /// </returns>
        ValueTask<TData> InvokeAsync(TData data, CancellationToken cancellationToken);
    }
}