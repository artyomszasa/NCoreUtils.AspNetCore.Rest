using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data;
using NCoreUtils.Linq;

namespace NCoreUtils.AspNetCore.Rest;

/// <summary>
/// Provides default implementation for REST CREATE method.
/// </summary>
/// <typeparam name="TData">Type of the target object.</typeparam>
/// <typeparam name="TId">Type of the Id property of the target object.</typeparam>
/// <remarks>
/// Initializes new instance from the specified parameters.
/// </remarks>
/// <param name="repository">Repository to use.</param>
/// <param name="logger">Logger to use.</param>
public class DefaultRestCreate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>(
    IDataRepository<TData, TId> repository,
    ILogger<DefaultRestCreate<TData, TId>> logger)
    : DefaultTransactedMethod<TData, TId>(repository)
    , IRestCreate<TData, TId>
    , IBoxedInvoke<IRestCreateContext<TData, TId>, TData>
    where TData : IHasId<TId>
{
    protected ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    object IBoxedInvoke.Instance => this;

    protected virtual bool HasValidId(TData data)
        => data.HasValidId();

    /// <summary>
    /// Performes REST CREATE action for the predefined type.
    /// </summary>
    /// <param name="context">Invocation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Object returned by dataset after insert operation. Depending on the repository implementation some of the values
    /// of the returned object may differ from the input.
    /// </returns>
#if NET7_0
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Handled by query provider.")]
    [UnconditionalSuppressMessage("Trim", "IL2026", Justification = "Handled by query provider.")]
#endif
    public virtual async ValueTask<TData> InvokeAsync(IRestCreateContext<TData, TId> context, CancellationToken cancellationToken)
    {
        var data = context.ThrowIfNull().Data;
        if (data.HasValidId())
        {
            // check if already exists
            if (await Repository.Items.AnyAsync(context.CreateIdEqualsPredicate(data.Id), cancellationToken))
            {
                Logger.LogRestEntityAlreadyExists(typeof(TData), data.Id);
                throw new ConflictException("Entity already exists.");
            }
        }
        // persist entity
        var result = await Repository.PersistAsync(data, cancellationToken);
        Logger.LogRestEntityCreatedSuccessfully(typeof(TData), result.Id);
        return result;
    }
}