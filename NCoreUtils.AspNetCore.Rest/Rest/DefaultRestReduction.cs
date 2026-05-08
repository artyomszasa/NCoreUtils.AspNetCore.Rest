using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data;
using NCoreUtils.Linq;

namespace NCoreUtils.AspNetCore.Rest;

public class DefaultRestReduction<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
    : IRestReduction<T>
    where T : class
{
    protected IDataRepository<T> Repository { get; }

    protected IRestQueryFilter<T> QueryFilter { get; }

    protected IRestQueryOrderer<T> QueryOrderer { get; }

    public DefaultRestReduction(
        IServiceProvider serviceProvider,
        IDataRepository<T> repository,
        IRestQueryFilter<T>? queryFilter = null,
        IRestQueryOrderer<T>? queryOrderer = null)
    {
        if (serviceProvider is null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        QueryFilter = queryFilter ?? ActivatorUtilities.CreateInstance<DefaultQueryFilter<T>>(serviceProvider);
        QueryOrderer = queryOrderer ?? ActivatorUtilities.CreateInstance<DefaultQueryOrderer<T>>(serviceProvider);
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Handled by query provider.")]
    [UnconditionalSuppressMessage("Trim", "IL2026", Justification = "Handled by query provider.")]
    protected virtual ValueTask<T?> ExecuteFirstOrDefaultAsync(IQueryable<T> queryable, CancellationToken cancellationToken = default)
        => new(queryable.FirstOrDefaultAsync(cancellationToken)!);

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Handled by query provider.")]
    [UnconditionalSuppressMessage("Trim", "IL2026", Justification = "Handled by query provider.")]
    protected virtual ValueTask<T?> ExecuteSingleOrDefaultAsync(IQueryable<T> queryable, CancellationToken cancellationToken = default)
        => new(queryable.SingleOrDefaultAsync(cancellationToken)!);

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Handled by query provider.")]
    [UnconditionalSuppressMessage("Trim", "IL2026", Justification = "Handled by query provider.")]
    protected virtual ValueTask<int> ExecuteCountAsync(IQueryable<T> queryable, CancellationToken cancellationToken = default)
        => new(queryable.CountAsync(cancellationToken)!);

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Handled by query provider.")]
    [UnconditionalSuppressMessage("Trim", "IL2026", Justification = "Handled by query provider.")]
    protected virtual ValueTask<bool> ExecuteAnyAsync(IQueryable<T> queryable, CancellationToken cancellationToken = default)
        => new(queryable.AnyAsync(cancellationToken)!);


    public async ValueTask<object?> InvokeAsync(IRestReductionContext<T> context, CancellationToken cancellationToken)
    {
        var reduction = context.ThrowIfNull().Reduction;
        var restQuery = context.RestQuery;
        var query = await Repository.Items
            // apply filters
            .Apply(QueryFilter, restQuery)
            // apply access limitations
            .ApplyAsync(context.AccessValidator, cancellationToken)
            .ConfigureAwait(false);
        var orderedQuery = query.Apply(QueryOrderer, restQuery);
        return reduction switch
        {
            DefaultReductions.First => await ExecuteFirstOrDefaultAsync(orderedQuery, cancellationToken).ConfigureAwait(false),
            DefaultReductions.Single => await ExecuteSingleOrDefaultAsync(orderedQuery, cancellationToken).ConfigureAwait(false),
            DefaultReductions.Count => await ExecuteCountAsync(orderedQuery, cancellationToken).ConfigureAwait(false),
            DefaultReductions.Any => await ExecuteAnyAsync(orderedQuery, cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException($"Reduction {reduction} is not supported")
        };
    }
}