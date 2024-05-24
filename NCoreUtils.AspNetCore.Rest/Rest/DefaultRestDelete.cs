using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest;

/// <summary>
/// Provides default implementation for REST DELETE method.
/// </summary>
/// <typeparam name="TData">Type of the target object.</typeparam>
/// <typeparam name="TId">Type of the Id property of the target object.</typeparam>
/// <remarks>
/// Initializes new instance from the specified parameters.
/// </remarks>
/// <param name="repository">Repository to use.</param>
/// <param name="logger">Logger to use.</param>
public class DefaultRestDelete<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>(
    IDataRepository<TData, TId> repository,
    ILogger<DefaultRestDelete<TData, TId>> logger)
    : DefaultTransactedMethod<TData, TId>(repository)
    , IRestDelete<TData, TId>
    , IBoxedVoidInvoke<IRestDeleteContext<TData, TId>>
    where TData : IHasId<TId>
{
    protected ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    object IBoxedInvoke.Instance => this;

    /// <summary>
    /// Performes REST DELETE action for the predefined type.
    /// </summary>
    /// <param name="context">Invocation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public virtual async ValueTask InvokeAsync(IRestDeleteContext<TData, TId> context, CancellationToken cancellationToken)
    {
        var id = context.Id;
        var item = await Repository.LookupAsync(id, cancellationToken);
        if (item is null)
        {
            Logger.LogRestNoEntityFound(typeof(TData), id);
            throw new NotFoundException();
        }
        await Repository.RemoveAsync(item, context.Force, cancellationToken: cancellationToken);
        Logger.LogRestEntityRemovedSuccessfully(typeof(TData), id);
    }
}