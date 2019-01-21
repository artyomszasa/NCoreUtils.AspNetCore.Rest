namespace NCoreUtils.AspNetCore.Rest

open System
open System.Collections.Concurrent
open System.Linq.Expressions
open System.Linq
open System.Reflection
open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open NCoreUtils
open NCoreUtils.Data
open System.Text.RegularExpressions

module internal OrderByApplier =

  type private IApplier =
    abstract OrderBy : IQueryable * LambdaExpression * isDescending:bool -> IOrderedQueryable
    abstract ThenBy : IOrderedQueryable * LambdaExpression * isDescending:bool -> IOrderedQueryable

  let private cache = ConcurrentDictionary<struct (Type * Type), IApplier> ()

  let private memberCache = ConcurrentDictionary<struct (Type * string), PropertyInfo> ()

  let private defaultPropertyCache = ConcurrentDictionary<Type, OrderByProperty> ()

  type private Applier<'a, 'key> () =
    interface IApplier with
      member __.OrderBy (source, selector, isDescending) =
        match source with
        | null -> ArgumentNullException "source" |> raise
        | :? IQueryable<'a> as source ->
          match selector with
          | null -> ArgumentNullException "selector" |> raise
          | :? Expression<Func<'a, 'key>> as selector ->
            match isDescending with
            | true -> source.OrderByDescending selector
            | _    -> source.OrderBy           selector
            :> IOrderedQueryable
          | _ -> invalidOp "Invalid selector."
        | _ -> invalidOp "Invalid source."
      member __.ThenBy (source, selector, isDescending) =
        match source with
        | null -> ArgumentNullException "source" |> raise
        | :? IOrderedQueryable<'a> as source ->
          match selector with
          | null -> ArgumentNullException "selector" |> raise
          | :? Expression<Func<'a, 'key>> as selector ->
            match isDescending with
            | true -> source.ThenByDescending selector
            | _    -> source.ThenBy           selector
            :> IOrderedQueryable
          | _ -> invalidOp "Invalid selector."
        | _ -> invalidOp "Invalid source."

  let private (|Selector|_|) (selector : LambdaExpression) =
    match selector.Parameters.Count with
    | 1 -> Some struct (selector.Parameters.[0].Type, selector.ReturnType)
    | _ -> None

  [<RequiresExplicitTypeArguments>]
  [<CompiledName("OrderBy")>]
  let orderBy<'a> (selector : LambdaExpression) (isDescending : bool) (source : IQueryable<'a>) =
    match selector with
    | null -> ArgumentNullException "selector" |> raise
    | Selector (struct (elementType, _) as cacheKey) when elementType = typeof<'a>  ->
      cache.GetOrAdd(cacheKey, fun (struct (elementType, keyType)) -> Activator.CreateInstance(typedefof<Applier<_, _>>.MakeGenericType (elementType, keyType), true) :?> IApplier)
        .OrderBy(source, selector, isDescending)
        :?> IOrderedQueryable<'a>
    | _ -> invalidOp "Invalid selector."

  [<RequiresExplicitTypeArguments>]
  [<CompiledName("ThenBy")>]
  let thenBy<'a> (selector : LambdaExpression) (isDescending : bool) (source : IOrderedQueryable<'a>) =
    match selector with
    | null -> ArgumentNullException "selector" |> raise
    | Selector (struct (elementType, _) as cacheKey) when elementType = typeof<'a>  ->
      cache.GetOrAdd(cacheKey, fun (struct (elementType, keyType)) -> Activator.CreateInstance(typedefof<Applier<_, _>>.MakeGenericType (elementType, keyType), true) :?> IApplier)
        .ThenBy(source, selector, isDescending)
        :?> IOrderedQueryable<'a>
    | _ -> invalidOp "Invalid selector."

  [<RequiresExplicitTypeArguments>]
  let private mkMemberSelector<'a> (memberName : string) =
    let propertyInfo =
      memberCache.GetOrAdd (
        struct (typeof<'a>, memberName),
        fun (struct (ty, name)) -> ty.GetProperty (name, BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.FlattenHierarchy ||| BindingFlags.IgnoreCase)
      )
    match propertyInfo with
    | null -> invalidOpf "No property \"%s\" is defined for type %s." memberName typeof<'a>.FullName
    | _ -> propertyInfo.CreateSelector ()

  [<RequiresExplicitTypeArguments>]
  [<CompiledName("OrderByMember")>]
  let orderByMember<'a> (memberName : string) isDescending source =
    orderBy<'a> (mkMemberSelector<'a> memberName) isDescending source

  [<RequiresExplicitTypeArguments>]
  [<CompiledName("ThenByMember")>]
  let thenByMember<'a> (memberName : string) isDescending source =
    thenBy<'a> (mkMemberSelector<'a> memberName) isDescending source

  [<RequiresExplicitTypeArguments>]
  [<CompiledName("OrderByDefaultMember")>]
  let orderByDefaultMember<'a> (serviceProvider : IServiceProvider) source =
    let { Property = propertyInfo; IsDescending = isDescending } =
      defaultPropertyCache.GetOrAdd (
        typeof<'a>,
        fun (_ : Type) ->
          let selector =
            tryGetService<IDefaultOrderProperty<'a>> serviceProvider
            |> Option.orElseWith (fun () -> tryGetService<IDefaultOrderProperty> serviceProvider >>| Adapt.For<'a>)
            |> Option.defaultWith (fun () -> DefaultDefaultOrderProperty.SharedInstance.For<'a>())
          selector.Select ()
      )
    orderBy<'a> (propertyInfo.CreateSelector ()) isDescending source

[<AutoOpen>]
module private DefaultQueryOrdererHelpers =

  let mayBeExpressionRegex = Regex ("[=><.+*/-]", RegexOptions.Compiled ||| RegexOptions.CultureInvariant)

  let inline getResult (choice : Choice<IQueryable<'a>, IOrderedQueryable<'a>>) =
    match choice with
    | Choice2Of2 result -> result
    | _                 -> invalidOp "should never happen"

  let inline mayBeExpression (input: string) = mayBeExpressionRegex.IsMatch input

/// Represents ordering option.
[<Struct>]
[<StructuralEquality; NoComparison>]
type OrderingOption = {
  /// Ordering criteria.
  By: string
  /// Ordereing direction.
  Direction: RestSortByDirection }

/// Provides default query ordering.
type DefaultQueryOrderer<'a> =
  val private serviceProvider : IServiceProvider

  val mutable private queryParser : IDataQueryExpressionBuilder voption

  member this.QueryParser
    with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () =
      match this.queryParser with
      | ValueNone ->
        let queryParser = this.serviceProvider.GetRequiredService<IDataQueryExpressionBuilder> ()
        this.queryParser <- ValueSome queryParser
        queryParser
      | ValueSome queryParser -> queryParser

  /// <summary>
  /// Initializes new instance from the specified parameters.
  /// </summary>
  /// <param name="serviceProvider">Service provider.</param>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  new (serviceProvider) =
    { serviceProvider = serviceProvider
      queryParser     = ValueNone }

  abstract GetOrderingOptions : restQuery:RestQuery -> seq<OrderingOption>

  abstract OrderQuery : queryable:IQueryable<'a> * restQuery:RestQuery -> IOrderedQueryable<'a>

  [<RequiresExplicitTypeArguments>]
  abstract OrderBy<'a> : by:string * isDescending:bool * IQueryable<'a> -> IOrderedQueryable<'a>

  [<RequiresExplicitTypeArguments>]
  abstract ThenBy<'a> : by:string * isDescending:bool * IOrderedQueryable<'a> -> IOrderedQueryable<'a>

  /// <summary>
  /// Normalizes ordering options if possible.
  /// </summary>
  /// <param name="restQuery">REST query.</param>
  /// <returns>Normalized ordering options.</returns>
  default __.GetOrderingOptions restQuery =
    match restQuery.SortBy.Length, restQuery.SortByDirection.Length with
    | 0, 0 -> Seq.empty
    | _, 0 ->
      let dir = RestSortByDirection.Asc
      restQuery.SortBy |> Seq.map (fun by -> { By = by.Trim (); Direction = dir })
    | _, 1 ->
      let dir = restQuery.SortByDirection.[0]
      restQuery.SortBy |> Seq.map (fun by -> { By = by.Trim (); Direction = dir })
    | a, b when a <= b ->
      Seq.map2 (fun (by : string) dir -> { By = by.Trim (); Direction = dir }) restQuery.SortBy restQuery.SortByDirection
    | _ -> invalidOpf "Invalid or ambigous ordering options (sortBy = %A, sortByDirection = %A)" restQuery.SortBy restQuery.SortByDirection

  /// <summary>
  /// Orders generic queryable using provided REST query.
  /// </summary>
  /// <typeparam name="a">Element type of the queryable.</typeparam>
  /// <param name="queryable">Source queryable.</param>
  /// <param name="restQuery">REST query to apply.</param>
  /// <returns>Ordered queryable.</returns>
  default this.OrderQuery (queryable : IQueryable<'a>, restQuery : RestQuery) =
    match restQuery.SortBy.Length with
    | 0 -> OrderByApplier.orderByDefaultMember<'a> this.serviceProvider queryable
    | _ ->
      this.GetOrderingOptions restQuery
      |> Seq.fold
        (fun q { By = by; Direction = dir } ->
          let isDescending = dir = RestSortByDirection.Desc
          match q with
          | Choice1Of2 queryable -> Choice2Of2 <| this.OrderBy<'a> (by, isDescending, queryable)
          | Choice2Of2 queryable -> Choice2Of2 <| this.ThenBy<'a>  (by, isDescending, queryable)
        )
        (Choice1Of2 queryable)
      |> getResult

  /// <summary>
  /// Applies ordering to the unordered query.
  /// </summary>
  /// <typeparam name="a">Element type of the queryable.</typeparam>
  /// <param name="by">Ordering criteria.</param>
  /// <param name="isDescending">Ordering direction.</param>
  /// <param name="queryable">Source queryable.</param>
  default this.OrderBy<'a> (by, isDescending, queryable) =
    match mayBeExpression by with
    | true ->
      try
        let lambda = this.QueryParser.BuildExpression (typeof<'a>, by)
        OrderByApplier.orderBy<'a> lambda isDescending queryable
      with exn ->
        let message = sprintf "SortBy expression contains special characters but could not be parsed as data expression: \"%s\"." by
        InvalidOperationException (message, exn) |> raise
    | _ -> OrderByApplier.orderByMember<'a> by isDescending queryable

  /// <summary>
  /// Applies additional ordering to the ordered query.
  /// </summary>
  /// <typeparam name="a">Element type of the queryable.</typeparam>
  /// <param name="by">Ordering criteria.</param>
  /// <param name="isDescending">Ordering direction.</param>
  /// <param name="queryable">Source queryable.</param>
  default this.ThenBy<'a> (by, isDescending, queryable) =
    match mayBeExpression by with
    | true ->
      try
        let lambda = this.QueryParser.BuildExpression (typeof<'a>, by)
        OrderByApplier.thenBy<'a> lambda isDescending queryable
      with exn ->
        let message = sprintf "SortBy expression contains special characters but could not be parsed as data expression: \"%s\"." by
        InvalidOperationException (message, exn) |> raise
    | _ -> OrderByApplier.thenByMember<'a> by isDescending queryable

  interface IRestQueryOrderer<'a> with
    member this.OrderQuery (queryable, restQuery) = this.OrderQuery (queryable, restQuery)

