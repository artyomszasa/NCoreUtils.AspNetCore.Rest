using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data;
using NCoreUtils.Linq;

namespace NCoreUtils.AspNetCore.Rest;

/// <summary>
/// Provides default implementation for REST LIST method.
/// </summary>
/// <typeparam name="TData">Type of the collection elements.</typeparam>
public class DefaultRestListCollection<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData> : IRestListCollection<TData>
{
    protected IDataRepository<TData> Repository { get; }

    protected IRestQueryFilter<TData> QueryFilter { get; }

    protected IRestQueryOrderer<TData> QueryOrderer { get; }

    public DefaultRestListCollection(
        IServiceProvider serviceProvider,
        IDataRepository<TData> repository,
        IRestQueryFilter<TData>? queryFilter = null,
        IRestQueryOrderer<TData>? queryOrderer = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(serviceProvider);
#else
        if (serviceProvider is null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }
#endif
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        QueryFilter = queryFilter ?? ActivatorUtilities.CreateInstance<DefaultQueryFilter<TData>>(serviceProvider);
        QueryOrderer = queryOrderer ?? ActivatorUtilities.CreateInstance<DefaultQueryOrderer<TData>>(serviceProvider);
    }

    /// <inheritdoc />
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Handled by query provider.")]
    [UnconditionalSuppressMessage("Trim", "IL2026", Justification = "Handled by query provider.")]
    public IAsyncEnumerable<TData> InvokeAsync(IRestListCollectionContext<TData> context, CancellationToken cancellationToken)
    {
        var restQuery = context.RestQuery;
        var filteredQueryTask = Repository.Items
            // apply filters
            .Apply(QueryFilter, restQuery)
            // apply access limitations
            .ApplyAsync(context.AccessValidator, cancellationToken);
        if (filteredQueryTask.IsCompletedSuccessfully)
        {
            var finalQuery = filteredQueryTask.Result
                .Apply(QueryOrderer, restQuery)
                .Skip(restQuery.GetOffset())
                .TakeWhenNonNegative(restQuery.GetCount());
            return finalQuery is IAsyncEnumerable<TData> asEnumerable ? asEnumerable : finalQuery.ExecuteAsync(cancellationToken);
        }

        return AsyncEnumerable.Delay(async (ctoken) =>
        {
            var sourceQuery = await filteredQueryTask;
            var finalQuery = sourceQuery
                .Apply(QueryOrderer, restQuery)
                .Skip(restQuery.GetOffset())
                .TakeWhenNonNegative(restQuery.GetCount());
            return finalQuery is IAsyncEnumerable<TData> asEnumerable ? asEnumerable : finalQuery.ExecuteAsync(ctoken);
        });
    }
}