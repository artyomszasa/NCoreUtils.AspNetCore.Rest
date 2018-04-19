namespace NCoreUtils.AspNetCore.Rest

open System
open System.Linq
open System.Linq.Expressions
open Microsoft.Extensions.DependencyInjection
open NCoreUtils.Data

/// <summary>
/// Provides default query filtering implementation. Uses <see cref="NCoreUtils.Data.IDataQueryExpressionBuilder" /> for
/// processing rest query conditions.
/// </summary>
type DefaultQueryFilter<'a> =
  val private serviceProvider : IServiceProvider

  /// <summary>
  /// Initializes new instance from the specified parameters.
  /// </summary>
  /// <param name="serviceProvider">Service provider.</param>
  new (serviceProvider) = { serviceProvider = serviceProvider }

  abstract FilterQuery : queryable:IQueryable<'a> * restQuery:RestQuery -> IQueryable<'a>

  /// <summary>
  /// Filters queryable using provided REST query.
  /// </summary>
  /// <param name="queryable">Source queryable.</param>
  /// <param name="restQuery">REST query to apply.</param>
  /// <returns>Filtered queryable.</returns>
  default this.FilterQuery (queryable : IQueryable<'a>, restQuery : RestQuery) =
    match String.IsNullOrEmpty restQuery.Filter with
    | true -> queryable
    | _ ->
      let expressionBuilder = this.serviceProvider.GetRequiredService<IDataQueryExpressionBuilder> ()
      let predicate = expressionBuilder.BuildExpression<'a>(restQuery.Filter) :?> Expression<Func<'a, bool>>
      Queryable.Where (queryable, predicate)

  interface IRestQueryFilter<'a> with
    member this.FilterQuery (queryable, restQuery) = this.FilterQuery (queryable, restQuery)
