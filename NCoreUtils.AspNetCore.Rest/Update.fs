namespace NCoreUtils.AspNetCore.Rest

open System
open System.Collections.Concurrent
open Microsoft.AspNetCore.Http
open NCoreUtils
open NCoreUtils.AspNetCore
open NCoreUtils.Data
open System.Runtime.CompilerServices

// **************************************************************************
// Update

type UpdateParameters = {
  [<ParameterBinder(typeof<ManagedTypeBinder>)>]
  EntityType : Type }

module internal UpdateInvoker =

  [<CompiledName("RestUpdate")>]
  [<RequiresExplicitTypeArguments>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let private update<'a, 'id when 'a :> IHasId<'id> and 'id : equality>
    (id : 'id)
    (httpContext : HttpContext)
    { ServiceProvider   = serviceProvider
      RestConfiguration = { AccessConfiguration = access }
      RestMethodInvoker = restMethodInvoker }
    (_ : UpdateParameters) = async {
      // --------------------------------
      // validate if method is accessible
      let! hasGlobalAccess = access.Global.AsyncValidate (serviceProvider, httpContext.User)
      do if not hasGlobalAccess then UnauthorizedException () |> raise
      let! hasMethodAccess = access.Update.AsyncValidate (serviceProvider, httpContext.User)
      do if not hasMethodAccess then UnauthorizedException () |> raise
      // initialize configured RestUpdate handler
      let instance =
        tryGetService<IRestUpdate<'a, 'id>> serviceProvider
        |> Option.orElseWith  (fun () -> tryGetService<IRestUpdate> serviceProvider >>| Adapt.For<'a, 'id>)
        |> Option.defaultWith (fun () -> diActivate<DefaultRestUpdate<'a, 'id>> serviceProvider :> _)
      // deserialize input
      let data =
        let deserializer =
          tryGetService<IDeserializer<'a>> serviceProvider
          |> Option.orElseWith  (fun () -> tryGetService<IDeserializer> serviceProvider >>| Adapt.For<'a>)
          |> Option.defaultWith (fun () -> diActivate<DefaultDeserializer<'a>> serviceProvider :> _)
        HttpContext.requestBody httpContext
        |> deserializer.Deserialize
      // check entity access if specified
      match access.Update with
      | :? IEntityAccessValidator as entityAccessValidator ->
        let! hasEntityAccess = entityAccessValidator.AsyncValidate (data, serviceProvider, httpContext.User)
        do if not hasEntityAccess then ForbiddenException () |> raise
      | _ -> ()
      // invoke method
      do!
        let boxed =
          match instance with
          | :? IBoxedInvoke<'id, 'a, 'a> as boxed -> boxed
          | _  -> { new IBoxedInvoke<_, _, _> with
                      member __.Instance = box instance
                      member __.AsyncInvoke (arg1, arg2) = instance.AsyncInvoke (arg1, arg2)
                  }
        RestMethodInvocation<'a, _, _, _> (boxed, id, data)
        |> restMethodInvoker.AsyncInvoke
        |> Async.Ignore
      // send response
      HttpContext.setResponseStatusCode 204 httpContext }

  type private IInvoker =
    abstract Invoke : rawId:string * httpContext:HttpContext * services:RestMethodServices * parameters:UpdateParameters -> Async<unit>

  [<Sealed>]
  type private Invoker<'a, 'id when 'a :> IHasId<'id> and 'id : equality> () =
    interface IInvoker with
      member __.Invoke (rawId, httpContext, services, parameters) =
        let id = Convert.ChangeType (rawId, typeof<'id>) :?> 'id
        update<'a, 'id> id httpContext services parameters

  let private cache = ConcurrentDictionary<Type, IInvoker> ()

  [<CompiledName("Invoke")>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let invoke rawId httpContext services (parameters : UpdateParameters) =
    let instance =
      cache.GetOrAdd (
        parameters.EntityType,
        fun ty -> typedefof<Invoker<_, _>>.MakeGenericType (ty, idType ty) |> activate :?> IInvoker)
    instance.Invoke (rawId, httpContext, services, parameters)


