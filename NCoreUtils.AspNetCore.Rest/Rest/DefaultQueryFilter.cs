using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using NCoreUtils.Data.Protocol;

namespace NCoreUtils.AspNetCore.Rest;

public class DefaultQueryFilter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(IDataQueryExpressionBuilder? queryExpressionBuilder = null) : IRestQueryFilter<T>
{
    private readonly IDataQueryExpressionBuilder? _queryExpressionBuilder = queryExpressionBuilder;

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Should be handled by the provider.")]
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
                var cbool = (bool)Convert.ChangeType(cbox, typeof(bool))!;
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