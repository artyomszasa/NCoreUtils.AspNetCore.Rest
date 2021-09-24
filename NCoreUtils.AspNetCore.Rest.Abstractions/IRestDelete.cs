using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Defines functionality to implement REST DELETE method for the concrete type.
    /// </summary>
    /// <typeparam name="TData">Type of the target object.</typeparam>
    /// <typeparam name="TId">Type of the Id property of the target object.</typeparam>
    public interface IRestDelete<TData, TId> : IRestTransactedMethod
        where TData : IHasId<TId>
    {
        /// <summary>
        /// Performes REST DELETE action for the predefined type.
        /// </summary>
        /// <param name="id">Id of the object to delete.</param>
        /// <param name="force">Whether to perform forced removal (see <c>NCoreUtils.Data</c>).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        ValueTask InvokeAsync(TId id, bool force, CancellationToken cancellationToken);
    }
}