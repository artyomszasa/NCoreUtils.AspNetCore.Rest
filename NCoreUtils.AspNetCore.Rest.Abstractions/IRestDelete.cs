using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest;

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
    /// <param name="context">REST invocation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask InvokeAsync(IRestDeleteContext<TData, TId> context, CancellationToken cancellationToken);
}