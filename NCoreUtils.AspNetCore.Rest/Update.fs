namespace NCoreUtils.AspNetCore.Rest

open System
open System.Collections.Concurrent
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open NCoreUtils
open NCoreUtils.AspNetCore
open NCoreUtils.Data
open System.Runtime.CompilerServices

// **************************************************************************
// Update

type UpdateParameters = {
  [<ParameterBinder(typeof<ManagedTypeBinder>)>]
  EntityType : Type }

type UpdateServices = IServiceProvider

module internal UpdateInvoker =

  [<RequiresExplicitTypeArguments>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let private update<'a, 'id when 'a :> IHasId<'id> and 'id : equality> (id : 'id) httpContext (services : UpdateServices) (_ : UpdateParameters) = async {
    let instance =
      tryGetService<IRestUpdate<'a, 'id>> services
      |> Option.orElseWith  (fun () -> tryGetService<IRestUpdate> services >>| Adapt.For<'a, 'id>)
      |> Option.defaultWith (fun () -> ActivatorUtilities.CreateInstance<DefaultRestUpdate<'a, 'id>> services :> _)
    let data =
      let deserializer =
        tryGetService<IDeserializer<'a>> services
        |> Option.orElseWith  (fun () -> tryGetService<IDeserializer> services >>| Adapt.For<'a>)
        |> Option.defaultWith (fun () -> ActivatorUtilities.CreateInstance<DefaultDeserializer<'a>> services :> _)
      HttpContext.requestBody httpContext
      |> deserializer.Deserialize
    use! tx = instance.AsyncBeginTransaction ()
    do! instance.AsyncInvoke (id, data) |> Async.Ignore
    tx.Commit ()
    HttpContext.setResponseStatusCode 204 httpContext }

  type private IInvoker =
    abstract Invoke : rawId:string * httpContext:HttpContext * services:UpdateServices * parameters:UpdateParameters -> Async<unit>

  type Invoker<'a, 'id when 'a :> IHasId<'id> and 'id : equality> () =
    interface IInvoker with
      member __.Invoke (rawId, httpContext, services, parameters) =
        let id = Convert.ChangeType (rawId, typeof<'id>) :?> 'id
        update<'a, 'id> id httpContext services parameters

  let private cache = ConcurrentDictionary<Type, IInvoker> ()

  let invoke rawId httpContext services (parameters : UpdateParameters) =
    let instance =
      cache.GetOrAdd (
        parameters.EntityType,
        fun ty -> typedefof<Invoker<_, _>>.MakeGenericType (ty, idType ty) |> activate :?> IInvoker)
    instance.Invoke (rawId, httpContext, services, parameters)


