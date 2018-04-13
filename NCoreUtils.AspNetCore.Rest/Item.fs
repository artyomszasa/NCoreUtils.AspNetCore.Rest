namespace NCoreUtils.AspNetCore.Rest

open System
open System.Collections.Concurrent
open Microsoft.AspNetCore.Http
open NCoreUtils
open NCoreUtils.AspNetCore
open NCoreUtils.Data
open System.Runtime.CompilerServices

// **************************************************************************
// ITEM


type ItemParameters = {
  [<ParameterBinder(typeof<ManagedTypeBinder>)>]
  EntityType : Type }

module internal ItemInvoker =

  [<CompiledName("RestItem")>]
  [<RequiresExplicitTypeArguments>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let private item<'a, 'id when 'a :> IHasId<'id> and 'id : equality>
    (id : 'id)
    (httpContext : HttpContext)
    { ServiceProvider = serviceProvider
      RestConfiguration = { AccessConfiguration = access } }
    (_ : ItemParameters) = async {
      // --------------------------------
      // validate if method is accessible
      let! hasGlobalAccess = access.Global.AsyncValidate (serviceProvider, httpContext.User)
      do if not hasGlobalAccess then UnauthorizedException () |> raise
      let! hasMethodAccess = access.Item.AsyncValidate (serviceProvider, httpContext.User)
      do if not hasMethodAccess then UnauthorizedException () |> raise
      // initialize configured RestItem handler
      let instance =
        tryGetService<IRestItem<'a, 'id>> serviceProvider
        |> Option.orElseWith  (fun () -> tryGetService<IRestItem> serviceProvider >>| Adapt.For<'a, 'id>)
        |> Option.defaultWith (fun () -> diActivate<DefaultRestItem<'a, 'id>> serviceProvider :> _)
      // invoke method
      let! item = instance.AsyncInvoke id
      // initialize configured serializer
      let serializer =
        tryGetService<ISerializer<'a>> serviceProvider
        |> Option.orElseWith  (fun () -> tryGetService<ISerializer> serviceProvider >>| Adapt.For<'a>)
        |> Option.defaultWith (fun () -> diActivate<DefaultSerializer<'a>> serviceProvider :> _)
      // serialize output
      let output = HttpContext.response httpContext |> HttpResponseOutput
      do! serializer.AsyncSerialize (output, item) }

  type private IInvoker =
    abstract Invoke : rawId:string * httpContext:HttpContext * services:RestMethodServices * parameters:ItemParameters -> Async<unit>

  [<Sealed>]
  type private Invoker<'a, 'id when 'a :> IHasId<'id> and 'id : equality> () =
    interface IInvoker with
      member __.Invoke (rawId, httpContext, services, parameters) =
        let id = Convert.ChangeType (rawId, typeof<'id>) :?> 'id
        item<'a, 'id> id httpContext services parameters

  let private cache = ConcurrentDictionary<Type, IInvoker> ()

  [<CompiledName("Invoke")>]
  let invoke rawId httpContext services (parameters : ItemParameters) =
    let instance =
      cache.GetOrAdd (
        parameters.EntityType,
        fun ty -> typedefof<Invoker<_, _>>.MakeGenericType (ty, idType ty) |> activate :?> IInvoker)
    instance.Invoke (rawId, httpContext, services, parameters)
