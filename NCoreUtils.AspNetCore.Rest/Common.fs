[<AutoOpen>]
module internal NCoreUtils.AspNetCore.Rest.Common

open System
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Primitives
open NCoreUtils
open NCoreUtils.AspNetCore
open NCoreUtils.Data
open System.Collections.Generic

let inline idType ty =
  let mutable idTy = Unchecked.defaultof<_>
  match IdUtils.TryGetIdType (ty, &idTy) with
  | true -> idTy
  | _    -> invalidOpf "Type %s does not implement IHasId interface." ty.FullName

let inline activate (ty : Type) = Activator.CreateInstance (ty, true)

[<RequiresExplicitTypeArguments>]
let inline diActivate<'a> serviceProvider = ActivatorUtilities.CreateInstance<'a> serviceProvider

let inline setResponseHeader (name : string) (value : string) httpContext =
  (HttpContext.response httpContext).Headers.Add (name, (StringValues : string -> _) value)

[<Sealed; AbstractClass>]
type internal Adapt =

  [<RequiresExplicitTypeArguments>]
  static member inline For<'a> (orderProperty : IDefaultOrderProperty) = orderProperty.For<'a> ()

  [<RequiresExplicitTypeArguments>]
  static member inline For<'a> (queryFilter : IRestQueryFilter) = queryFilter.For<'a> ()

  [<RequiresExplicitTypeArguments>]
  static member inline For<'a> (queryOrderer : IRestQueryOrderer) = queryOrderer.For<'a> ()

  [<RequiresExplicitTypeArguments>]
  static member inline For<'a> (deserializer : IDeserializer) = deserializer.For<'a> ()

  [<RequiresExplicitTypeArguments>]
  static member inline For<'a> (serializer : ISerializer) = serializer.For<'a> ()

  [<RequiresExplicitTypeArguments>]
  static member inline For<'a, 'id when 'a :> IHasId<'id> and 'id : equality> (item : IRestItem) = item.For<'a, 'id> ()

  [<RequiresExplicitTypeArguments>]
  static member inline For<'a, 'id when 'a :> IHasId<'id> and 'id : equality> (create : IRestCreate) = create.For<'a, 'id> ()

  [<RequiresExplicitTypeArguments>]
  static member inline For<'a, 'id when 'a :> IHasId<'id> and 'id : equality> (update : IRestUpdate) = update.For<'a, 'id> ()

  [<RequiresExplicitTypeArguments>]
  static member inline For<'a, 'id when 'a :> IHasId<'id> and 'id : equality> (delete : IRestDelete) = delete.For<'a, 'id> ()

  [<RequiresExplicitTypeArguments>]
  static member inline For<'a> (list : IRestListCollection) = list.For<'a> ()

[<Interface>]
type IBoxedInvoke<'TArg, 'TResult> =
  abstract Instance : obj
  abstract AsyncInvoke : arg:'TArg -> Async<'TResult>

[<Interface>]
type IBoxedInvoke<'TArg1, 'TArg2, 'TResult> =
  abstract Instance : obj
  abstract AsyncInvoke : arg1:'TArg1 * arg2:'TArg2 -> Async<'TResult>

[<AbstractClass>]
type RestMethodInvocation<'TItem, 'TResult> () =
  inherit RestMethodInvocation<'TResult> ()
  override __.ItemType = typeof<'TItem>

[<Sealed>]
type RestMethodInvocation<'TItem, 'TArg, 'TResult> (b : IBoxedInvoke<'TArg, 'TResult>, arg : 'TArg) =
  inherit RestMethodInvocation<'TItem, 'TResult> ()
  let arguments = [| box arg |] :> IReadOnlyList<_>
  override __.Arguments = arguments
  override __.Instance = b.Instance
  override __.AsyncInvoke () = b.AsyncInvoke arg

[<Sealed>]
type RestMethodInvocation<'TItem, 'TArg1, 'TArg2, 'TResult> (b : IBoxedInvoke<'TArg1, 'TArg2, 'TResult>, arg1 : 'TArg1, arg2 : 'TArg2) =
  inherit RestMethodInvocation<'TItem, 'TResult> ()
  let arguments = [| box arg1; box arg2 |] :> IReadOnlyList<_>
  override __.Arguments = arguments
  override __.Instance = b.Instance
  override __.AsyncInvoke () = b.AsyncInvoke (arg1, arg2)

