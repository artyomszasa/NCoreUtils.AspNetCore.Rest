namespace NCoreUtils

open System
open System.Collections.Concurrent
open System.Collections.Immutable
open NCoreUtils
open Microsoft.Extensions.DependencyInjection

[<AbstractClass>]
type internal ImmutableArrayBinder (serviceProvider : IServiceProvider, itemBinder : TypeOrInstance option) =
  static let getArrayElementType (ty : Type) =
    match ty.IsGenericType with
    | false -> None
    | _ ->
    match ty.GetGenericTypeDefinition () with
    | tydef when tydef = typedefof<ImmutableArray<_>> -> Some ty.GenericTypeArguments.[0]
    | _                                               -> None
  member __.AsyncBindParameters<'item> (itemBinder : IValueBinder, descriptor : ParameterDescriptor, tryGetParameters : string -> string list option) =
    match tryGetParameters descriptor.Path with
    | None
    | Some []
    | Some [ "" ] -> ImmutableArray<'item>.Empty |> async.Return
    | Some values ->
      let descriptor' = { descriptor with Type = typeof<'item> }
      values
      |> Seq.map
        (fun value ->
          let tryGetParameters' key =
            match key = descriptor.Path with
            | false -> None
            | true  -> Some <| [ value ]
          itemBinder.AsyncBind (descriptor', tryGetParameters'))
      |> Seq.toArray
      |> Async.Sequential
      >>| (Seq.cast<'item> >> ImmutableArray.CreateRange)
  interface IValueBinder with
    member this.AsyncBind (descriptor, tryGetParameters) =
      match getArrayElementType descriptor.Type with
      | None ->
        invalidOpf "ImmutableArrayBinder can only bind ImmutableArray types, %s was requested." descriptor.Type.FullName
      | Some itemType ->
        let binder =
          match itemBinder |> Option.orElseWith (fun () -> ParameterBinding.tryGetDefaultBinder itemType) with
          | Some (BinderInstance binder) -> binder
          | Some (BinderType binderType) -> ActivatorUtilities.CreateInstance (serviceProvider, binderType) :?> IValueBinder
          | _ -> invalidOpf "No item binder could be found for %s." itemType.FullName
        AsyncBindInvoker.Invoke (this, itemType, binder, descriptor, tryGetParameters)

and [<AbstractClass>] private AsyncBindInvoker () =
  static let cache = ConcurrentDictionary<Type, AsyncBindInvoker> ()
  abstract Invoke : instance:ImmutableArrayBinder * itemBinder:IValueBinder * descriptor:ParameterDescriptor * tryGetParameters:(string -> string list option) -> Async<obj>
  static member Invoke (instance : ImmutableArrayBinder, itemType : Type, itemBinder:IValueBinder, descriptor:ParameterDescriptor, tryGetParameters:(string -> string list option)) =
    cache.GetOrAdd(itemType, fun ty -> Activator.CreateInstance (typedefof<AsyncBindInvoker<_>>.MakeGenericType ty, true) :?> AsyncBindInvoker)
      .Invoke(instance, itemBinder, descriptor, tryGetParameters)

and [<Sealed>] private AsyncBindInvoker<'item> () =
  inherit AsyncBindInvoker ()
  override __.Invoke (instance, itemBinder, descriptor, tryGetParameters) = async {
    let! value = instance.AsyncBindParameters<'item> (itemBinder, descriptor, tryGetParameters)
    return box value }

[<Sealed>]
type internal DefaultImmutableArrayBinder (serviceProvider : IServiceProvider) =
  inherit ImmutableArrayBinder (serviceProvider, None)
