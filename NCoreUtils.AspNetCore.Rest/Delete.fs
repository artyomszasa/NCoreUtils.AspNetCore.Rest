namespace NCoreUtils.AspNetCore.Rest

open System
open System.Collections.Concurrent
open Microsoft.AspNetCore.Http
open NCoreUtils
open NCoreUtils.AspNetCore
open NCoreUtils.Data
open System.Runtime.CompilerServices

// **************************************************************************
// DELETE

/// Bound parameters for REST DELETE method.
type DeleteParameters = {
  /// Type of the entity being deleted.
  [<ParameterBinder(typeof<ManagedTypeBinder>)>]
  EntityType : Type }

module internal DeleteInvoker =

  [<CompiledName("RestDelete")>]
  [<RequiresExplicitTypeArguments>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let private delete<'a, 'id when 'a :> IHasId<'id> and 'id : equality>
    (id : 'id)
    (httpContext : HttpContext)
    { ServiceProvider   = serviceProvider
      RestConfiguration = { AccessConfiguration = access } }
    (_ : DeleteParameters) = async {
      // --------------------------------
      // validate if method is accessible
      let! hasGlobalAccess = access.Global.AsyncValidate (serviceProvider, httpContext.User)
      do if not hasGlobalAccess then UnauthorizedException () |> raise
      let! hasMethodAccess = access.Delete.AsyncValidate (serviceProvider, httpContext.User)
      do if not hasMethodAccess then UnauthorizedException () |> raise
      // initialize configured RestDelete handler
      let instance =
        tryGetService<IRestDelete<'a, 'id>> serviceProvider
        |> Option.orElseWith  (fun () -> tryGetService<IRestDelete> serviceProvider >>| Adapt.For<'a, 'id>)
        |> Option.defaultWith (fun () -> diActivate<DefaultRestDelete<'a, 'id>> serviceProvider :> _)
      // invoke method within transaction
      use! tx = instance.AsyncBeginTransaction ()
      do! instance.AsyncInvoke id
      tx.Commit ()
      // send response
      HttpContext.setResponseStatusCode 204 httpContext }

  type private IInvoker =
    abstract Invoke : rawId:string * httpContext:HttpContext * services:RestMethodServices * parameters:DeleteParameters -> Async<unit>

  [<Sealed>]
  type private Invoker<'a, 'id when 'a :> IHasId<'id> and 'id : equality> () =
    interface IInvoker with
      member __.Invoke (rawId, httpContext, services, parameters) =
        let id = Convert.ChangeType (rawId, typeof<'id>) :?> 'id
        delete<'a, 'id> id httpContext services parameters

  let private cache = ConcurrentDictionary<Type, IInvoker> ()

  [<CompiledName("Invoke")>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let invoke rawId httpContext services (parameters : DeleteParameters) =
    let instance =
      cache.GetOrAdd (
        parameters.EntityType,
        fun ty -> typedefof<Invoker<_, _>>.MakeGenericType (ty, idType ty) |> activate :?> IInvoker)
    instance.Invoke (rawId, httpContext, services, parameters)


