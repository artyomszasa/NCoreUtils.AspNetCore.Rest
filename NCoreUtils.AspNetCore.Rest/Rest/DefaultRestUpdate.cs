using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data;
using NCoreUtils.Linq;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Provides default implementation for REST UPDATE method.
    /// </summary>
    /// <typeparam name="TData">Type of the target object.</typeparam>
    /// <typeparam name="TId">Type of the Id property of the target object.</typeparam>
    public class DefaultRestUpdate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>
        : DefaultTransactedMethod<TData, TId>, IRestUpdate<TData, TId>, IBoxedInvoke<TId, TData, TData>
        where TData : IHasId<TId>
    {
        readonly ILogger _logger;

        object IBoxedInvoke.Instance => this;

        /// <summary>
        /// Initializes new instance from the specified parameters.
        /// </summary>
        /// <param name="repository">Repository to use.</param>
        /// <param name="logger">Logger to use.</param>
        public DefaultRestUpdate(IDataRepository<TData, TId> repository, ILogger<DefaultRestUpdate<TData, TId>> logger)
            : base(repository)
            => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <summary>
        /// Performes REST UPDATE action for the specified type.
        /// </summary>
        /// <param name="id">Id of the object to update.</param>
        /// <param name="data">Object to update in dataset.</param>
        /// <returns>
        /// Object returned by dataset after update operation. Depending on the repository implementation some of the values
        /// of the returned object may differ from the input.
        /// </returns>
        public async ValueTask<TData> InvokeAsync(TId id, TData data, CancellationToken cancellationToken)
        {
            // check that data has the same id
            if (!EqualityComparer<TId>.Default.Equals(id, data.Id))
            {
                throw new BadRequestException("Entity data has invalid id.");
            }
            if (!(await Repository.Items.AnyAsync(QueryableExtensions.CreateIdEqualityPredicate<TData, TId>(id), cancellationToken)))
            {
                throw new NotFoundException();
            }
            var result = await Repository.PersistAsync(data, cancellationToken);
            _logger.LogInformation("Entity of type {0} with key = {1} has been updated (rest-update).", typeof(TData), id);
            return result;
        }
    }
}