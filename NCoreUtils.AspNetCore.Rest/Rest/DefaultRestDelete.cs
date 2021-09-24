using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Provides default implementation for REST DELETE method.
    /// </summary>
    /// <typeparam name="TData">Type of the target object.</typeparam>
    /// <typeparam name="TId">Type of the Id property of the target object.</typeparam>
    public class DefaultRestDelete<TData, TId>
        : DefaultTransactedMethod<TData, TId>, IRestDelete<TData, TId>, IBoxedVoidInvoke<TId, bool>
        where TData : IHasId<TId>
    {
        protected ILogger Logger { get; }

        object IBoxedInvoke.Instance => this;

        /// <summary>
        /// Initializes new instance from the specified parameters.
        /// </summary>
        /// <param name="repository">Repository to use.</param>
        /// <param name="logger">Logger to use.</param>
        public DefaultRestDelete(IDataRepository<TData, TId> repository, ILogger<DefaultRestDelete<TData, TId>> logger)
            : base(repository)
            => Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <summary>
        /// Performes REST DELETE action for the predefined type.
        /// </summary>
        /// <param name="id">Id of the object to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public virtual async ValueTask InvokeAsync(TId id, bool force, CancellationToken cancellationToken)
        {
            var item = await Repository.LookupAsync(id, cancellationToken);
            if (item is null)
            {
                Logger.LogDebug("No entity of type {0} found for key = {1} (rest-delete).", typeof(TData), id);
                throw new NotFoundException();
            }
            await Repository.RemoveAsync(item, cancellationToken: cancellationToken);
            Logger.LogInformation("Successfully removed entity of type {0} with key = {1} (data-delete).", typeof(TData), id);
        }
    }
}