using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace NCoreUtils.AspNetCore.Rest;

/// <summary>
/// Defines functionality to implement REST LIST method for the concrete type.
/// </summary>
/// <typeparam name="T">Type of the target object.</typeparam>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public interface IRestListCollection<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    /// <summary>
    /// Performes REST LIST action for the predefined type with the specified parameters.
    /// </summary>
    /// <param name="context">Rest invocation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// REST LIST response that contains partial resultset defined by Offset and Count properties of the rest query
    /// parameter.
    /// </returns>
    IAsyncEnumerable<T> InvokeAsync(IRestListCollectionContext<T> context, CancellationToken cancellationToken);
}