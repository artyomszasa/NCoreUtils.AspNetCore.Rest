using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest;

/// <summary>
/// Defines functionality to implement REST UPDATE method for the concrete type.
/// </summary>
/// <typeparam name="TData">Type of the target object.</typeparam>
/// <typeparam name="TId">Type of the Id property of the target object.</typeparam>
public interface IRestUpdate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId> : IRestTransactedMethod
    where TData : IHasId<TId>
{
    /// <summary>
    /// Performes REST UPDATE action for the predefined type.
    /// </summary>
    /// <param name="context">Rest invocation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Object returned by dataset after update operation. Depending on the dataset implementation some of the values of
    /// the returned object may differ from the input.
    /// </returns>
    ValueTask<TData> InvokeAsync(IRestUpdateContext<TData, TId> context, CancellationToken cancellationToken);
}