namespace NCoreUtils.AspNetCore.Rest

open System
open System.Collections.Concurrent
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open NCoreUtils
open NCoreUtils.AspNetCore
open NCoreUtils.Data
open System.IO
open System.Runtime.CompilerServices

// **************************************************************************
// ITEM


type ItemParameters = {
  [<ParameterBinder(typeof<ManagedTypeBinder>)>]
  EntityType : Type }

type ItemServices = IServiceProvider

module internal ItemInvoker =

  [<RequiresExplicitTypeArguments>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let private item<'a, 'id when 'a :> IHasId<'id> and 'id : equality> (id : 'id) httpContext (services : ItemServices) (_ : ItemParameters) = async {
    let instance =
      tryGetService<IRestItem<'a, 'id>> services
      |> Option.orElseWith  (fun () -> tryGetService<IRestItem> services >>| Adapt.For<'a, 'id>)
      |> Option.defaultWith (fun () -> ActivatorUtilities.CreateInstance<DefaultRestItem<'a, 'id>> services :> _)
    let! item = instance.AsyncInvoke id
    let serializer =
      tryGetService<ISerializer<'a>> services
      |> Option.orElseWith  (fun () -> tryGetService<ISerializer> services >>| Adapt.For<'a>)
      |> Option.defaultWith (fun () -> ActivatorUtilities.CreateInstance<DefaultSerializer<'a>> services :> _)
    let output = HttpContext.response httpContext |> HttpResponseOutput
    do! serializer.AsyncSerialize (output, item) }

  type private IInvoker =
    abstract Invoke : rawId:string * httpContext:HttpContext * services:ItemServices * parameters:ItemParameters -> Async<unit>

  type Invoker<'a, 'id when 'a :> IHasId<'id> and 'id : equality> () =
    interface IInvoker with
      member __.Invoke (rawId, httpContext, services, parameters) =
        let id = Convert.ChangeType (rawId, typeof<'id>) :?> 'id
        item<'a, 'id> id httpContext services parameters

  let private cache = ConcurrentDictionary<Type, IInvoker> ()

  let invoke rawId httpContext services (parameters : ItemParameters) =
    let instance =
      cache.GetOrAdd (
        parameters.EntityType,
        fun ty -> typedefof<Invoker<_, _>>.MakeGenericType (ty, idType ty) |> activate :?> IInvoker)
    instance.Invoke (rawId, httpContext, services, parameters)
