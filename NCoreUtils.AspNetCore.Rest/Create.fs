namespace NCoreUtils.AspNetCore.Rest

open System
open System.Collections.Concurrent
open System.Runtime.CompilerServices
open Microsoft.AspNetCore.Http
open NCoreUtils
open NCoreUtils.AspNetCore
open NCoreUtils.Data
open Common

// **************************************************************************
// CREATE

/// Bound parameters for REST CREATE method.
type CreateParameters = {
  /// Type of the entity being created.
  [<ParameterBinder(typeof<ManagedTypeBinder>)>]
  EntityType : Type }

module internal CreateInvoker =

  [<CompiledName("RestCreate")>]
  [<RequiresExplicitTypeArguments>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let private create<'a, 'id when 'a :> IHasId<'id> and 'id : equality>
    (httpContext : HttpContext)
    { ServiceProvider   = serviceProvider
      CurrentTypeName   = { Value = typeName }
      RestConfiguration = { AccessConfiguration = access; PathPrefix = pathPrefix }
      RestMethodInvoker = restMethodInvoker }
    (_ : CreateParameters) = async {
      // --------------------------------
      // validate if method is accessible
      let! hasGlobalAccess = access.Global.AsyncValidate (serviceProvider, httpContext.User)
      do if not hasGlobalAccess then UnauthorizedException () |> raise
      let! hasMethodAccess = access.Create.AsyncValidate (serviceProvider, httpContext.User)
      do if not hasMethodAccess then UnauthorizedException () |> raise
      // initialize configured RestCreate handler
      let instance =
        tryGetService<IRestCreate<'a, 'id>> serviceProvider
        |> Option.orElseWith  (fun () -> tryGetService<IRestCreate> serviceProvider >>| Adapt.For<'a, 'id>)
        |> Option.defaultWith (fun () -> diActivate<DefaultRestCreate<'a, 'id>> serviceProvider :> _)
      // deserialize input
      let data =
        let deserializer =
          tryGetService<IDeserializer<'a>> serviceProvider
          |> Option.orElseWith  (fun () -> tryGetService<IDeserializer> serviceProvider >>| Adapt.For<'a>)
          |> Option.defaultWith (fun () -> diActivate<DefaultDeserializer<'a>> serviceProvider :> _)
        HttpContext.requestBody httpContext
        |> deserializer.Deserialize
      // check entity access if specified
      match access.Create with
      | :? IEntityAccessValidator as entityAccessValidator ->
        let! hasEntityAccess = entityAccessValidator.AsyncValidate (data, serviceProvider, httpContext.User)
        do if not hasEntityAccess then ForbiddenException () |> raise
      | _ -> ()
      // invoke method using invoker
      let! item =
        let boxed =
          match instance with
          | :? IBoxedInvoke<'a, 'a> as boxed -> boxed
          | _  -> { new IBoxedInvoke<'a, 'a> with
                      member __.Instance = box instance
                      member __.AsyncInvoke arg = instance.AsyncInvoke arg
                  }
        RestMethodInvocation<'a, 'a, 'a> (boxed, data)
        |> restMethodInvoker.AsyncInvoke
      // send response
      let request = HttpContext.request httpContext
      let struct (host, port) =
        match request.Host.HasValue with
        | true  -> struct (request.Host.Host, request.Host.Port |? -1)
        | false -> struct ("localhost", -1)
      let uri =
        let builder =
          UriBuilder (
            Scheme = request.Scheme,
            Host   = host,
            Port   = port,
            Path   = sprintf "%s%s/%s"
                      (pathPrefix |> Seq.map (fun ci -> ci.Value) |> String.concat "/" |> sprintf "%s/")
                      typeName.Value
                      (item.Id.ToString ()))
        builder.Uri
      setResponseHeader Headers.Location uri.AbsoluteUri httpContext
      HttpContext.setResponseStatusCode 201 httpContext }

  type private IInvoker =
    abstract Invoke : httpContext:HttpContext * services:RestMethodServices * parameters:CreateParameters -> Async<unit>

  [<Sealed>]
  type private Invoker<'a, 'id when 'a :> IHasId<'id> and 'id : equality> () =
    interface IInvoker with
      member __.Invoke (httpContext, services, parameters) =
        create<'a, 'id> httpContext services parameters

  let private cache = ConcurrentDictionary<Type, IInvoker> ()

  [<CompiledName("Invoke")>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let invoke httpContext services (parameters : CreateParameters) =
    let instance =
      cache.GetOrAdd (
        parameters.EntityType,
        fun ty -> typedefof<Invoker<_, _>>.MakeGenericType (ty, idType ty) |> activate :?> IInvoker)
    instance.Invoke (httpContext, services, parameters)


