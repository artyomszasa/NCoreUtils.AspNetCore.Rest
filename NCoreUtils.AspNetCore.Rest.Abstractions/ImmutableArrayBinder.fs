#nowarn "9"
namespace NCoreUtils

open System
open System.Collections.Concurrent
open System.Collections.Immutable
open NCoreUtils
open Microsoft.Extensions.DependencyInjection
open Microsoft.FSharp.NativeInterop

[<AutoOpen>]
module private ImmutableArrayBinderHelpers =

  let splitString (input: string) =
    match input with
    | null | "" -> Seq.empty
    | _ ->
    match input.IndexOf '"' with
    | -1 -> input.Split ',' |> Seq.ofArray
    | _  ->
      let buffer = NativePtr.stackalloc<char> 8192
      let mutable offset = 0
      let inline push char =
        NativePtr.set buffer offset char
        offset <- offset + 1
      let len = input.Length
      let segments = ResizeArray 4
      let mutable index = 0
      let mutable quoted = false
      let mutable escaped = false // may only be true when within escaped context
      while index < len do
        let ch = input.[index]
        match ch with
        | ',' when not quoted ->
          // no quotes, just comma --> push segment
          if 0 <> offset then
            segments.Add(new System.String (buffer, 0, offset))
            offset <- 0
        | '"' ->
          match quoted with
          | false ->
            // quote starts here
            quoted <- true
          | _ ->
          match escaped with
          | true ->
            // escaped quote within quoted expression --> push quote
            push '"'
            escaped <- false
          | _ ->
            // quote ends here
            quoted <- false
        | '\\' ->
          match quoted with
          | false ->
            // treat as normal char
            push '\\'
          | _ ->
          match escaped with
          | false ->
            // next char is escaped
            escaped <- true
          | true ->
            // escaped escape char within quoted context
            push '\\'
        | _ ->
          // normal char
          match escaped with
          | true ->
            // normal char within quoted escaped context
            push '\\'
            push ch
          | false ->
            // normal char either in quoted or unquoted context
            push ch
        index <- index + 1
      if 0 <> offset then
        segments.Add(new System.String (buffer, 0, offset))
        offset <- 0
      segments :> _

  let getSplitted =
    function
    | None -> ValueNone
    | Some values ->
      values
      |> Seq.collect splitString
      |> ValueSome


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
    match tryGetParameters descriptor.Path |> getSplitted with
    | ValueNone -> ImmutableArray<'item>.Empty |> async.Return
    | ValueSome values ->
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
