namespace NCoreUtils.AspNetCore.Rest

open System
open System.Diagnostics.CodeAnalysis
open System.Linq
open System.Runtime.CompilerServices
open NCoreUtils
open NCoreUtils.Data
open NCoreUtils.Linq
open Microsoft.Extensions.DependencyInjection

[<AutoOpen>]
module private DefaultRestListCollectionHelpers =
  [<ExcludeFromCodeCoverage>]
  let inline unboxQuery (queryable : IQueryable) = queryable :?> IQueryable<'a>

/// <summary>
/// Provides default implementation for REST LIST method.
/// </summary>
/// <typeparam name="a">Type of the target object.</typeparam>
type DefaultRestListCollection<'a>  =
  val mutable serviceProvider : IServiceProvider
  val mutable repository : IDataRepository<'a>

  /// <summary>
  /// Initializes new instance from the specified parameters.
  /// </summary>
  /// <param name="serviceProvider">Service provider.</param>
  /// <param name="repository">Repository to use.</param>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  new (serviceProvider, repository) =
    { serviceProvider = serviceProvider
      repository = repository }

  abstract AsyncInvoke : restQuery:RestQuery * accessValidator:(IQueryable -> Async<IQueryable>) -> Async<struct ('a[] * int)>

  /// <summary>
  /// Performes REST LIST action for the specified type with the specified parameters.
  /// </summary>
  /// <typeparam name="a">Type of the target object.</typeparam>
  /// <param name="restQuery">Query options specified in the request.</param>
  /// <param name="accessValidator">
  /// Queryable decorator that filters out non-accessible entities depending on the access configuration.
  /// </param>
  /// <returns>
  /// REST LIST response that contains partial resultset defined by Offset and Count properties of the rest query
  /// parameter, and total entity count for the conditions specified by the rest query parameter.
  /// </returns>
  default this.AsyncInvoke (restQuery, accessValidator) = async {
    // create filter applier
    let applyFilters =
      let instance =
        tryGetService<IRestQueryFilter<'a>> this.serviceProvider
        |> Option.orElseWith  (fun () -> tryGetService<IRestQueryFilter> this.serviceProvider >>| (fun selector -> selector.For<'a>()))
        |> Option.defaultWith (fun () -> ActivatorUtilities.CreateInstance<DefaultQueryFilter<'a>> this.serviceProvider :> _)
      fun queryable ->
        instance.FilterQuery (queryable, restQuery)
        |>  accessValidator
        >>| unboxQuery
    // create ordering applier
    let applyOrdering =
      let instance =
        tryGetService<IRestQueryOrderer<'a>> this.serviceProvider
        |> Option.orElseWith  (fun () -> tryGetService<IRestQueryOrderer> this.serviceProvider >>| (fun selector -> selector.For<'a>()))
        |> Option.defaultWith (fun () -> ActivatorUtilities.CreateInstance<DefaultQueryOrderer<'a>> this.serviceProvider :> _)
      fun queryable -> instance.OrderQuery (queryable, restQuery)
    // execute queries
    let! query = this.repository.Items |> applyFilters
    let! total = Q.asyncCount query
    let! items =
      query
      |> applyOrdering
      |> Q.skip restQuery.Offset
      |> Q.take restQuery.Count
      |> Q.asyncToArray
    return struct (items, total) }

  interface IRestListCollection<'a> with
    member this.AsyncInvoke (restQuery, accessValidator) = this.AsyncInvoke (restQuery, accessValidator)
  interface IBoxedInvoke<RestQuery, Linq.IQueryable -> Async<Linq.IQueryable>, struct ('a[] * int)> with
    member this.Instance = box this
    member this.AsyncInvoke (restQuery, accessValidator) = this.AsyncInvoke (restQuery, accessValidator)