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
        Preconditions.ThrowIfNull(restQuery);
        if (!restQuery.SortBy.HasValue || 0 == restQuery.SortBy.Value.Count)
        {
            yield break;
        }
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

    protected virtual IOrderedQueryable<T> ApplyOrder(IQueryable<T> source, OrderingOption orderingOption)
    {
        if (MaybeExpression(orderingOption.By))
        {
            var queryExpressionBuilder = _serviceProvider.GetOptionalService<IDataQueryExpressionBuilder>()
                ?? throw new InvalidOperationException("Default rest query orderer requires NCoreUtils data query services in order to parse expression based ordering.");
            try
            {
                var lambda = queryExpressionBuilder.BuildExpression(typeof(T), orderingOption.By);
                return OrderBy(source, lambda, orderingOption.IsDescending);
            }
            catch (Exception exn)
            {
                throw new InvalidOperationException($"SortBy expression contains special characters but could not be parsed as data expression: \"{orderingOption.By}\".", exn);
            }
        }
        return OrderBy(source, orderingOption.By, orderingOption.IsDescending);
    }

    protected virtual IOrderedQueryable<T> ApplyFurtherOrder(IOrderedQueryable<T> source, OrderingOption orderingOption)
    {
        if (MaybeExpression(orderingOption.By))
        {
            var queryExpressionBuilder = _serviceProvider.GetOptionalService<IDataQueryExpressionBuilder>()
                ?? throw new InvalidOperationException("Default rest query orderer requires NCoreUtils data query services in order to parse expression based ordering.");
            try
            {
                var lambda = queryExpressionBuilder.BuildExpression(typeof(T), orderingOption.By);
                return ThenBy(source, lambda, orderingOption.IsDescending);
            }
            catch (Exception exn)
            {
                throw new InvalidOperationException($"SortBy expression contains special characters but could not be parsed as data expression: \"{orderingOption.By}\".", exn);
            }
        }
        return ThenBy(source, orderingOption.By, orderingOption.IsDescending);
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