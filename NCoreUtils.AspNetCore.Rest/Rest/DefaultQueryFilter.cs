using System;
using System.Linq;
using System.Linq.Expressions;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest
{
    public class DefaultQueryFilter<T> : IRestQueryFilter<T>
    {
        readonly IDataQueryExpressionBuilder? _queryExpressionBuilder;

        public DefaultQueryFilter(IDataQueryExpressionBuilder? queryExpressionBuilder = null)
        {
            _queryExpressionBuilder = queryExpressionBuilder;
        }

        public IQueryable<T> ApplyFilters(IQueryable<T> source, RestQuery restQuery)
        {
            if (string.IsNullOrWhiteSpace(restQuery.Filter))
            {
                return source;
            }
            if (_queryExpressionBuilder is null)
            {
                throw new InvalidOperationException("Default rest query filter requires NCoreUtils data query services in order to parse filters.");
            }
            Expression<Func<T, bool>> predicate;
            try
            {
                var expression = _queryExpressionBuilder.BuildExpression(typeof(T), restQuery.Filter);
                if (expression.Body.TryExtractConstant(out var cbox))
                {
                    var cbool = (bool)Convert.ChangeType(cbox, typeof(bool));
                    predicate = Expression.Lambda<Func<T, bool>>(QueryableExtensions.BoxConstant(cbool), expression.Parameters);
                }
                else
                {
                    predicate = (Expression<Func<T, bool>>)expression;
                }
            }
            catch (Exception exn)
            {
                throw new BadRequestException($"Invalid filter has been specified.", exn);
            }
            return source.Where(predicate);
        }
    }
}