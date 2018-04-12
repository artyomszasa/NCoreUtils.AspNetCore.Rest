namespace NCoreUtils.AspNetCore.Rest

open System
open System.Collections.Concurrent
open System.Runtime.CompilerServices
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open NCoreUtils
open NCoreUtils.AspNetCore
open NCoreUtils.Data

// **************************************************************************
// CREATE

/// Bound parameters for REST CREATE method.
type CreateParameters = {
  /// Type of the entity being created.
  [<ParameterBinder(typeof<ManagedTypeBinder>)>]
  EntityType : Type }

/// Bound services for REST CREATE method.
type CreateServices = {
  /// Local service provider to be used to resolve REST services.
  ServiceProvider   : IServiceProvider
  /// Holds unresolved type name of the entity being created.
  CurrentTypeName   : CurrentRestTypeName
  /// REST configuration.
  RestConfiguration : RestConfiguration }

module internal CreateInvoker =

  [<RequiresExplicitTypeArguments>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let private create<'a, 'id when 'a :> IHasId<'id> and 'id : equality> httpContext (services : CreateServices) (_ : CreateParameters) = async {
    // initialize configured RestCreate handler
    let instance =
      tryGetService<IRestCreate<'a, 'id>> services.ServiceProvider
      |> Option.orElseWith  (fun () -> tryGetService<IRestCreate> services.ServiceProvider >>| Adapt.For<'a, 'id>)
      |> Option.defaultWith (fun () -> ActivatorUtilities.CreateInstance<DefaultRestCreate<'a, 'id>> services.ServiceProvider :> _)
    // deserialize input
    let data =
      let deserializer =
        tryGetService<IDeserializer<'a>> services.ServiceProvider
        |> Option.orElseWith  (fun () -> tryGetService<IDeserializer> services.ServiceProvider >>| Adapt.For<'a>)
        |> Option.defaultWith (fun () -> ActivatorUtilities.CreateInstance<DefaultDeserializer<'a>> services.ServiceProvider :> _)
      HttpContext.requestBody httpContext
      |> deserializer.Deserialize
    // invoke method within transaction
    let! item = async {
      use! tx = instance.AsyncBeginTransaction ()
      let! item = instance.AsyncInvoke data
      tx.Commit ()
      return item }
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
                    (services.RestConfiguration.PathPrefix |> Seq.map (fun ci -> ci.Value) |> String.concat "/" |> sprintf "%s/")
                    services.CurrentTypeName.Value.Value
                    (item.Id.ToString ()))
      builder.Uri
    setResponseHeader Headers.Location uri.AbsoluteUri httpContext
    HttpContext.setResponseStatusCode 201 httpContext }

  type private IInvoker =
    abstract Invoke : httpContext:HttpContext * services:CreateServices * parameters:CreateParameters -> Async<unit>

  type Invoker<'a, 'id when 'a :> IHasId<'id> and 'id : equality> () =
    interface IInvoker with
      member __.Invoke (httpContext, services, parameters) =
        create<'a, 'id> httpContext services parameters

  let private cache = ConcurrentDictionary<Type, IInvoker> ()

  let invoke httpContext services (parameters : CreateParameters) =
    let instance =
      cache.GetOrAdd (
        parameters.EntityType,
        fun ty -> typedefof<Invoker<_, _>>.MakeGenericType (ty, idType ty) |> activate :?> IInvoker)
    instance.Invoke (httpContext, services, parameters)


