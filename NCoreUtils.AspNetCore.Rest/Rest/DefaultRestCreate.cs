using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data;
using NCoreUtils.Linq;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Provides default implementation for REST CREATE method.
    /// </summary>
    /// <typeparam name="TData">Type of the target object.</typeparam>
    /// <typeparam name="TId">Type of the Id property of the target object.</typeparam>
    public class DefaultRestCreate<TData, TId>
        : DefaultTransactedMethod<TData, TId>, IRestCreate<TData, TId>, IBoxedInvoke<TData, TData>
        where TData : IHasId<TId>
    {
        protected ILogger Logger { get; }

        object IBoxedInvoke.Instance => this;

        /// <summary>
        /// Initializes new instance from the specified parameters.
        /// </summary>
        /// <param name="repository">Repository to use.</param>
        /// <param name="logger">Logger to use.</param>
        public DefaultRestCreate(IDataRepository<TData, TId> repository, ILogger<DefaultRestCreate<TData, TId>> logger)
            : base(repository)
            => Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        protected virtual bool HasValidId(TData data)
            => data.HasValidId();

        /// <summary>
        /// Performes REST CREATE action for the predefined type.
        /// </summary>
        /// <param name="data">Object to insert into dataset.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// Object returned by dataset after insert operation. Depending on the repository implementation some of the values
        /// of the returned object may differ from the input.
        /// </returns>
        public virtual async ValueTask<TData> InvokeAsync(TData data, CancellationToken cancellationToken)
        {
            if (data.HasValidId())
            {
                // check if already exists
                if (await Repository.Items.AnyAsync(QueryableExtensions.CreateIdEqualityPredicate<TData, TId>(data.Id), cancellationToken))
                {
                    Logger.LogDebug("Entity of type {0} with key = {1} already exists (rest-create).", typeof(TData), data.Id);
                    throw new ConflictException("Entity already exists.");
                }
            }
            // persist entity
            var result = await Repository.PersistAsync(data, cancellationToken);
            Logger.LogInformation("Entity of type {0} has been created with key = {1} (rest-create).", typeof(TData), result.Id);
            return result;
        }
    }
}