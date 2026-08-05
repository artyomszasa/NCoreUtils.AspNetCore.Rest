using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest;

/// <summary>
/// Provides base class for default implementation of transacted REST methods.
/// </summary>
/// <typeparam name="TData">Type of the target object.</typeparam>
/// <typeparam name="TId">Type of the Id property of the target object.</typeparam>
/// <remarks>
/// Initializes new instance from the specified parameters.
/// </remarks>
/// <param name="repository">Repository to use.</param>
public abstract class DefaultTransactedMethod<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>(IDataRepository<TData, TId> repository)
    : IRestTransactedMethod
    where TData : IHasId<TId>
{
    /// Gets underlying data repository.
    protected IDataRepository<TData, TId> Repository { get; } = repository ?? throw new ArgumentNullException(nameof(repository));

    /// <summary>
    /// Initiates transaction through the underlying data repository context.
    /// </summary>
    /// <returns>Transaction to use.</returns>
    public virtual ValueTask<IDataTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        => Repository.Context.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);
}