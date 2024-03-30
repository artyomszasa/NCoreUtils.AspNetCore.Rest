using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest;

/// <summary>
/// Defines functionality to implement REST ITEM method for concrete type.
/// </summary>
/// <typeparam name="TData">Type of the target object.</typeparam>
/// <typeparam name="TId">Type of the Id property of the target object.</typeparam>
public interface IRestItem<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>
    where TData : IHasId<TId>
{
    /// <summary>
    /// Performes REST ITEM action for the predefined type.
    /// </summary>
    /// <param name="context">REST invocation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Object of the specified type for the specified id.
    /// </returns>
    ValueTask<TData> InvokeAsync(IRestItemContext<TData, TId> context, CancellationToken cancellationToken);
}