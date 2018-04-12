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

type DeleteParameters = {
  [<ParameterBinder(typeof<ManagedTypeBinder>)>]
  EntityType : Type }

type DeleteServices = IServiceProvider

module internal DeleteInvoker =

  [<RequiresExplicitTypeArguments>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let private delete<'a, 'id when 'a :> IHasId<'id> and 'id : equality> (id : 'id) httpContext (services : DeleteServices) (_ : DeleteParameters) = async {
    let instance =
      tryGetService<IRestDelete<'a, 'id>> services
      |> Option.orElseWith  (fun () -> tryGetService<IRestDelete> services >>| Adapt.For<'a, 'id>)
      |> Option.defaultWith (fun () -> ActivatorUtilities.CreateInstance<DefaultRestDelete<'a, 'id>> services :> _)
    use! tx = instance.AsyncBeginTransaction ()
    do! instance.AsyncInvoke id
    tx.Commit ()
    HttpContext.setResponseStatusCode 204 httpContext }

  type private IInvoker =
    abstract Invoke : rawId:string * httpContext:HttpContext * services:DeleteServices * parameters:DeleteParameters -> Async<unit>

  type Invoker<'a, 'id when 'a :> IHasId<'id> and 'id : equality> () =
    interface IInvoker with
      member __.Invoke (rawId, httpContext, services, parameters) =
        let id = Convert.ChangeType (rawId, typeof<'id>) :?> 'id
        delete<'a, 'id> id httpContext services parameters

  let private cache = ConcurrentDictionary<Type, IInvoker> ()

  let invoke rawId httpContext services (parameters : DeleteParameters) =
    let instance =
      cache.GetOrAdd (
        parameters.EntityType,
        fun ty -> typedefof<Invoker<_, _>>.MakeGenericType (ty, idType ty) |> activate :?> IInvoker)
    instance.Invoke (rawId, httpContext, services, parameters)


