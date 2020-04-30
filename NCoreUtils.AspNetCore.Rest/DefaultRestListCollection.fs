namespace NCoreUtils.AspNetCore.Rest

open System
open System.Diagnostics
open System.Diagnostics.CodeAnalysis
open System.Linq
open System.Runtime.CompilerServices
open NCoreUtils
open NCoreUtils.Data
open NCoreUtils.Linq
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

[<AutoOpen>]
module private DefaultRestListCollectionHelpers =
  [<ExcludeFromCodeCoverage>]
  let inline unboxQuery (queryable : IQueryable) = queryable :?> IQueryable<'a>

/// <summary>
/// Provides default generic implementation for REST LIST method.
/// </summary>
/// <typeparam name="a">Type of the target object.</typeparam>
[<AbstractClass>]
type DefaultRestListCollectionBase<'a> =
  val mutable serviceProvider: IServiceProvider

  new (serviceProvider) =
    { serviceProvider = serviceProvider }

  abstract CreateQuery: unit -> IQueryable<'a>

  abstract AsyncInvoke : restQuery:RestQuery * accessValidator:(IQueryable -> Async<IQueryable>) -> Async<struct ('a[] * int)>

  /// <summary>
  /// Performes REST LIST action for the specified type with the specified parameters.
  /// </summary>
  /// <param name="restQuery">Query options specified in the request.</param>
  /// <param name="accessValidator">
  /// Queryable decorator that filters out non-accessible entities depending on the access configuration.
  /// </param>
  /// <returns>
  /// REST LIST response that contains partial resultset defined by Offset and Count properties of the rest query
  /// parameter, and total entity count for the conditions specified by the rest query parameter.
  /// </returns>
  default this.AsyncInvoke (restQuery, accessValidator) = async {
    let logger = this.serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger (this.GetType().Name)
    let stopwatch = Stopwatch ()
    stopwatch.Start ()
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
    let prepareMs = stopwatch.ElapsedMilliseconds
    // execute queries
    let! query = this.CreateQuery () |> applyFilters
    let! total = Q.asyncCount query
    let totalMs = stopwatch.ElapsedMilliseconds - prepareMs
    let! items =
      query
      |> applyOrdering
      |> Q.skip restQuery.Offset
      |> Q.take restQuery.Count
      |> Q.asyncToArray
    stopwatch.Stop ()
    let overallMs = stopwatch.ElapsedMilliseconds
    logger.LogInformation (
      "REST LIST operation executed (prepare took {0}ms, total query took {1}ms, item query took {2}ms, overall time {3}ms)",
      prepareMs,
      totalMs,
      overallMs - totalMs,
      totalMs
    )
    return struct (items, total) }

  interface IRestListCollection<'a> with
    member this.AsyncInvoke (restQuery, accessValidator) = this.AsyncInvoke (restQuery, accessValidator)
  interface IBoxedInvoke<RestQuery, Linq.IQueryable -> Async<Linq.IQueryable>, struct ('a[] * int)> with
    member this.Instance = box this
    member this.AsyncInvoke (restQuery, accessValidator) = this.AsyncInvoke (restQuery, accessValidator)

/// <summary>
/// Provides default repository based implementation for REST LIST method.
/// </summary>
/// <typeparam name="a">Type of the target object.</typeparam>
type DefaultRestListCollection<'a>  =
  inherit DefaultRestListCollectionBase<'a>
  val mutable repository : IDataRepository<'a>

  /// <summary>
  /// Initializes new instance from the specified parameters.
  /// </summary>
  /// <param name="serviceProvider">Service provider.</param>
  /// <param name="repository">Repository to use.</param>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  new (serviceProvider, repository) =
    { inherit DefaultRestListCollectionBase<'a> (serviceProvider)
      repository = repository }

  override this.CreateQuery () = this.repository.Items
