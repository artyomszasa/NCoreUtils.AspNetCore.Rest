using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data;
using NCoreUtils.Linq;

namespace NCoreUtils.AspNetCore.Rest;

/// <summary>
/// Provides default implementation for REST ITEM method.
/// </summary>
/// <typeparam name="TData">Type of the target object.</typeparam>
/// <typeparam name="TId">Type of the Id property of the target object.</typeparam>
/// <remarks>
/// Initializes new instance from the specified parameters.
/// </remarks>
/// <param name="repository">Repository to use.</param>
public class DefaultRestItem<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>(IDataRepository<TData, TId> repository)
    : IRestItem<TData, TId>
    , IBoxedInvoke<IRestItemContext<TData, TId>, TData?>
    where TData : IHasId<TId>
{
    object IBoxedInvoke.Instance => this;

    /// Gets underlying data repository.
    protected IDataRepository<TData, TId> Repository { get; } = repository ?? throw new ArgumentNullException(nameof(repository));

    /// <summary>
    /// Performes REST ITEM action for the predefined type.
    /// </summary>
    /// <param name="context">Invocation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Object of the specified type for the specified id.
    /// </returns>
#if NET7_0
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Handled by query provider.")]
    [UnconditionalSuppressMessage("Trim", "IL2026", Justification = "Handled by query provider.")]
#endif
    public async ValueTask<TData?> InvokeAsync(IRestItemContext<TData, TId> context, CancellationToken cancellationToken)
    {
        var query = Repository.Items.Where(context.CreateIdEqualsPredicate(context.Id));
        var accessibleQuery = (IQueryable<TData>)await context.AccessValidator(query, cancellationToken);
        var item = await accessibleQuery.FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException();
        return item;
    }
}