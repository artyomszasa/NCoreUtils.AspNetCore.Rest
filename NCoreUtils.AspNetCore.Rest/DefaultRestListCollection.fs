namespace NCoreUtils.AspNetCore.Rest

open System
open NCoreUtils
open NCoreUtils.Data
open NCoreUtils.Linq
open Microsoft.Extensions.DependencyInjection

type DefaultRestListCollection<'a> (serviceProvider : IServiceProvider, repository : IDataRepository<'a>) =
  interface IRestListCollection<'a> with
    member __.AsyncInvoke (restQuery) = async {
      // create filter applier
      let applyFilters =
        let instance =
          tryGetService<IRestQueryFilter<'a>> serviceProvider
          |> Option.orElseWith  (fun () -> tryGetService<IRestQueryFilter> serviceProvider >>| (fun selector -> selector.For<'a>()))
          |> Option.defaultWith (fun () -> ActivatorUtilities.CreateInstance<DefaultQueryFilter<'a>> serviceProvider :> _)
        fun queryable -> instance.FilterQuery (queryable, restQuery)
      // create ordering applier
      let applyOrdering =
        let instance =
          tryGetService<IRestQueryOrderer<'a>> serviceProvider
          |> Option.orElseWith  (fun () -> tryGetService<IRestQueryOrderer> serviceProvider >>| (fun selector -> selector.For<'a>()))
          |> Option.defaultWith (fun () -> ActivatorUtilities.CreateInstance<DefaultQueryOrderer<'a>> serviceProvider :> _)
        fun queryable -> instance.OrderQuery (queryable, restQuery)
      // execute queries
      let  query = repository.Items |> applyFilters
      let! total = Q.asyncCount query
      let! items =
        query
        |> applyOrdering
        |> Q.skip restQuery.Offset
        |> Q.take restQuery.Count
        |> Q.asyncToArray
      return struct (items, total) }
