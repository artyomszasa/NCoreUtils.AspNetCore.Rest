using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data;
using NCoreUtils.Linq;

namespace NCoreUtils.AspNetCore.Rest;

/// <summary>
/// Provides default implementation for REST UPDATE method.
/// </summary>
/// <typeparam name="TData">Type of the target object.</typeparam>
/// <typeparam name="TId">Type of the Id property of the target object.</typeparam>
/// <remarks>
/// Initializes new instance from the specified parameters.
/// </remarks>
/// <param name="repository">Repository to use.</param>
/// <param name="logger">Logger to use.</param>
public class DefaultRestUpdate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>(
    IDataRepository<TData, TId> repository,
    ILogger<DefaultRestUpdate<TData, TId>> logger)
    : DefaultTransactedMethod<TData, TId>(repository)
    , IRestUpdate<TData, TId>
    , IBoxedInvoke<IRestUpdateContext<TData, TId>, TData>
    where TData : IHasId<TId>
{
    protected ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

#pragma warning disable CA1033 // Interface methods should be callable by child types
    object IBoxedInvoke.Instance => this;
#pragma warning restore CA1033 // Interface methods should be callable by child types

    /// <summary>
    /// Performes REST UPDATE action for the specified type.
    /// </summary>
    /// <param name="context">Invocation context.</param>
    /// <param name="data">Object to update in dataset.</param>
    /// <returns>
    /// Object returned by dataset after update operation. Depending on the repository implementation some of the values
    /// of the returned object may differ from the input.
    /// </returns>
#if NET7_0
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Handled by query provider.")]
    [UnconditionalSuppressMessage("Trim", "IL2026", Justification = "Handled by query provider.")]
#endif
    public async ValueTask<TData> InvokeAsync(IRestUpdateContext<TData, TId> context, CancellationToken cancellationToken)
    {
        var id = context.ThrowIfNull().Id;
        var data = context.Data;
        // check that data has the same id
        if (!EqualityComparer<TId>.Default.Equals(id, data.Id))
        {
            throw new BadRequestException("Entity data has invalid id.");
        }
        if (!await Repository.Items.AnyAsync(context.CreateIdEqualsPredicate(id), cancellationToken).ConfigureAwait(false))
        {
            throw new NotFoundException();
        }
        var result = await Repository.PersistAsync(data, cancellationToken).ConfigureAwait(false);
        Logger.LogRestEntityUpdatedSuccessfully(typeof(TData), id);
        return result;
    }
}