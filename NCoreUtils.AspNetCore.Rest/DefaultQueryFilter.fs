namespace NCoreUtils.AspNetCore.Rest

open System
open System.Linq
open System.Linq.Expressions
open Microsoft.Extensions.DependencyInjection
open NCoreUtils.Data

type DefaultQueryFilter<'a> (serviceProvider : IServiceProvider) =
  interface IRestQueryFilter<'a> with
    member __.FilterQuery (queryable : IQueryable<'a>, restQuery : RestQuery) =
      match String.IsNullOrEmpty restQuery.Filter with
      | true -> queryable
      | _ ->
        let expressionBuilder = serviceProvider.GetRequiredService<IDataQueryExpressionBuilder> ()
        let predicate = expressionBuilder.BuildExpression<'a>(restQuery.Filter) :?> Expression<Func<'a, bool>>
        Queryable.Where (queryable, predicate)