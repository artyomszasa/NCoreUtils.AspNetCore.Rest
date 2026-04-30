using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NCoreUtils.Data.Protocol;

namespace NCoreUtils.AspNetCore.Rest;

public abstract partial class DefaultQueryOrderer { }

/// <summary>
/// Implements default query ordering.
/// <para>
/// Accepts either property names or expression based key selectors. Processing expression based key selectors
/// requires NCoreUtils data protocol services.
/// </para>
/// </summary>
public class DefaultQueryOrderer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(IServiceProvider serviceProvider)
    : DefaultQueryOrderer
    , IRestQueryOrderer<T>
{
    private static bool MaybeExpression(ReadOnlySpan<char> source)
    {
        if (source.Length == 0)
        {
            return false;
        }
        foreach (var ch in source)
        {
            if (ch == '=' || ch == '>' || ch == '<' || ch == '.' || ch == '+' || ch == '*' || ch == '/' || ch == '-')
            {
                return true;
            }
        }
        return false;
    }

    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    protected virtual IEnumerable<OrderingOption> GetOrderingOptions(RestQuery restQuery)
    {
#pragma warning disable CS8629 // Nullable value type may be null.
        if (!restQuery.ThrowIfNull().SortBy.HasValue || 0 == restQuery.SortBy.Value.Count)
        {
            yield break;
        }
#pragma warning restore CS8629 // Nullable value type may be null.
        if (!restQuery.SortByDirections.HasValue || 0 == restQuery.SortByDirections.Value.Count)
        {
            foreach (var by in restQuery.SortBy.Value)
            {
                yield return new OrderingOption(by, false);
            }
            yield break;
        }
        if (1 == restQuery.SortByDirections.Value.Count)
        {
            var isDescending = restQuery.SortByDirections.Value[0] == RestSortByDirection.Desc;
            foreach (var by in restQuery.SortBy)
            {
                yield return new OrderingOption(by, isDescending);
            }
            yield break;
        }
        if (restQuery.SortBy.Value.Count > restQuery.SortByDirections.Value.Count)
        {
            var bys = string.Join(", ", restQuery.SortBy);
            var dirs = string.Join(", ", restQuery.SortByDirections);
            throw new InvalidOperationException($"Invalid or ambigous ordering options (sortBy = {bys}, sortByDirection = {dirs}).");
        }
        for (var i = 0; i < restQuery.SortBy.Value.Count; ++i)
        {
            yield return new OrderingOption(restQuery.SortBy.Value[i], restQuery.SortByDirections.Value[i] == RestSortByDirection.Desc);
        }
    }

#pragma warning disable CA1716 // Identifiers should not match keywords
    protected virtual IOrderedQueryable<T> ApplyOrder(IQueryable<T> source, OrderingOption option)
#pragma warning restore CA1716 // Identifiers should not match keywords
    {
        if (MaybeExpression(option.By))
        {
            var queryExpressionBuilder = _serviceProvider.GetOptionalService<IDataQueryExpressionBuilder>()
                ?? throw new InvalidOperationException("Default rest query orderer requires NCoreUtils data query services in order to parse expression based ordering.");
            try
            {
                var lambda = queryExpressionBuilder.BuildExpression(typeof(T), option.By);
                return OrderBy(source, lambda, option.IsDescending);
            }
            catch (Exception exn)
            {
                throw new InvalidOperationException($"SortBy expression contains special characters but could not be parsed as data expression: \"{option.By}\".", exn);
            }
        }
        return OrderBy(source, option.By, option.IsDescending);
    }

#pragma warning disable CA1716 // Identifiers should not match keywords
    protected virtual IOrderedQueryable<T> ApplyFurtherOrder(IOrderedQueryable<T> source, OrderingOption option)
#pragma warning restore CA1716 // Identifiers should not match keywords
    {
        if (MaybeExpression(option.By))
        {
            var queryExpressionBuilder = _serviceProvider.GetOptionalService<IDataQueryExpressionBuilder>()
                ?? throw new InvalidOperationException("Default rest query orderer requires NCoreUtils data query services in order to parse expression based ordering.");
            try
            {
                var lambda = queryExpressionBuilder.BuildExpression(typeof(T), option.By);
                return ThenBy(source, lambda, option.IsDescending);
            }
            catch (Exception exn)
            {
                throw new InvalidOperationException($"SortBy expression contains special characters but could not be parsed as data expression: \"{option.By}\".", exn);
            }
        }
        return ThenBy(source, option.By, option.IsDescending);
    }

    public IQueryable<T> ApplyOrder(IQueryable<T> source, RestQuery restQuery)
    {
        var options = GetOrderingOptions(restQuery);
        IOrderedQueryable<T>? result = null;
        foreach (var option in options)
        {
            result = result is null ? ApplyOrder(source, option) : ApplyFurtherOrder(result, option);
        }
        return result ?? OrderByDefaultProperty(source, _serviceProvider);
    }
}