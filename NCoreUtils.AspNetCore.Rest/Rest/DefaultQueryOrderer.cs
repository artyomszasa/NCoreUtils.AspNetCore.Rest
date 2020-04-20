using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest
{
    public abstract partial class DefaultQueryOrderer { }

    /// <summary>
    /// Implements default query ordering.
    /// <para>
    /// Accepts either property names or expression based key selectors. Processing expression based key selectors
    /// requires NCoreUtils data protocol services.
    /// </para>
    /// </summary>
    public class DefaultQueryOrderer<T> : DefaultQueryOrderer, IRestQueryOrderer<T>
    {
        static readonly Regex mayBeExpressionRegex = new Regex("[=><.+*/-]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        readonly IServiceProvider _serviceProvider;

        public DefaultQueryOrderer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        protected virtual IEnumerable<OrderingOption> GetOrderingOptions(RestQuery restQuery)
        {
            if (0 == restQuery.SortBy.Count)
            {
                yield break;
            }
            if (1 == restQuery.SortByDirection.Count)
            {
                var isDescending = restQuery.SortByDirection[0] == RestSortByDirection.Desc;
                for (var i = 0; i < restQuery.SortBy.Count; ++i)
                {
                    yield return new OrderingOption(restQuery.SortBy[i], isDescending);
                }
                yield break;
            }
            if (restQuery.SortBy.Count > restQuery.SortByDirection.Count)
            {
                throw new InvalidOperationException($"Invalid or ambigous ordering options (sortBy = {restQuery.SortBy}, sortByDirection = {restQuery.SortByDirection}).");
            }
            for (var i = 0; i < restQuery.SortBy.Count; ++i)
            {
                yield return new OrderingOption(restQuery.SortBy[i], restQuery.SortByDirection[i] == RestSortByDirection.Desc);
            }
        }

        protected virtual IOrderedQueryable<T> ApplyOrder(IQueryable<T> source, OrderingOption option)
        {
            if (mayBeExpressionRegex.IsMatch(option.By))
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

        protected virtual IOrderedQueryable<T> ApplyFurtherOrder(IOrderedQueryable<T> source, OrderingOption option)
        {
            if (mayBeExpressionRegex.IsMatch(option.By))
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

        public IOrderedQueryable<T> ApplyOrder(IQueryable<T> source, RestQuery restQuery)
        {
            var options = GetOrderingOptions(restQuery);
            IOrderedQueryable<T>? result = null;
            foreach (var option in options)
            {
                result = result is null ? ApplyOrder(source, option) : ApplyFurtherOrder(result, option);
            }
            return result ?? OrderByDefaultProperty<T>(source, _serviceProvider);
        }
    }
}