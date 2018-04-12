namespace NCoreUtils.AspNetCore.Rest

open System
open System.Collections.Concurrent
open System.Linq.Expressions
open System.Linq
open System.Reflection
open NCoreUtils
open System.Runtime.CompilerServices

module OrderByApplier =

  type private IApplier =
    abstract OrderBy : IQueryable * LambdaExpression * isDescending:bool -> IOrderedQueryable
    abstract ThenBy : IOrderedQueryable * LambdaExpression * isDescending:bool -> IOrderedQueryable

  let private cache = ConcurrentDictionary<struct (Type * Type), IApplier> ()

  let private memberCache = ConcurrentDictionary<struct (Type * string), PropertyInfo> ()

  let private defaultPropertyCache = ConcurrentDictionary<Type, struct (PropertyInfo * bool)> ()

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
    | null -> invalidOpf "No property %s is defined for type %s." typeof<'a>.FullName memberName
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
    let struct (propertyInfo, isDescending) =
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


type DefaultQueryOrderer<'a> (serviceProvider : IServiceProvider) =
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static let getResult (choice : Choice<IQueryable<'a>, IOrderedQueryable<'a>>) =
    match choice with
    | Choice2Of2 result -> result
    | _                 -> invalidOp "should never happen"
  interface IRestQueryOrderer<'a> with
    member __.OrderQuery (queryable : IQueryable<'a>, restQuery : RestQuery) =
      match restQuery.SortBy.Length with
      | 0 -> OrderByApplier.orderByDefaultMember<'a> serviceProvider queryable
      | _ ->
        Seq.zip restQuery.SortBy restQuery.SortByDirection
        |> Seq.fold
          (fun q (by, dir) ->
            match q with
            | Choice1Of2 queryable -> Choice2Of2 <| OrderByApplier.orderByMember<'a> by (dir = RestSortByDirection.Desc) queryable
            | Choice2Of2 queryable -> Choice2Of2 <| OrderByApplier.thenByMember<'a>  by (dir = RestSortByDirection.Desc) queryable
          )
          (Choice1Of2 queryable)
        |> getResult

