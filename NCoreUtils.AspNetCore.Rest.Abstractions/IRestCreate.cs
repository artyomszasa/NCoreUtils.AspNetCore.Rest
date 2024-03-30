using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest;

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
    /// <param name="context">REST invocation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Object returned by the dataset after insert operation. Depending on the dataset implementation the returned
    /// object or its properties may not be the same as the input object.
    /// </returns>
    ValueTask<TData> InvokeAsync(IRestCreateContext<TData, TId> context, CancellationToken cancellationToken);
}