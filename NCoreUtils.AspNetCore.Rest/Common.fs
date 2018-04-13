[<AutoOpen>]
module internal NCoreUtils.AspNetCore.Rest.Common

open System
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Primitives
open NCoreUtils
open NCoreUtils.AspNetCore
open NCoreUtils.Data

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
