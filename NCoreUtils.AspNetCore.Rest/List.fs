namespace NCoreUtils.AspNetCore.Rest

open System
open System.Collections.Concurrent
open Microsoft.AspNetCore.Http
open NCoreUtils
open NCoreUtils.AspNetCore
open System.Runtime.CompilerServices

// **************************************************************************
// LIST COLLECTION

/// Bound parameters for REST LIST method.
[<NoEquality; NoComparison>]
type ListParameters = {
  /// Type of the entity being queried.
  [<ParameterBinder(typeof<ManagedTypeBinder>)>]
  EntityType : Type
  /// Quering options from the request.
  [<ParameterName("")>]
  RestQuery  : RestQuery }

module internal ListInvoker =

  [<CompiledName("RestList")>]
  [<RequiresExplicitTypeArguments>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let private list<'a>
    (httpContext : HttpContext)
    { ServiceProvider   = serviceProvider
      RestConfiguration = { AccessConfiguration = access }
      RestMethodInvoker = restMethodInvoker }
    (parameters : ListParameters) = async {
      // --------------------------------
      // validate if method is accessible
      let! hasGlobalAccess = access.Global.AsyncValidate (serviceProvider, httpContext.User)
      do if not hasGlobalAccess then UnauthorizedException () |> raise
      let! hasMethodAccess = access.List.AsyncValidate (serviceProvider, httpContext.User)
      do if not hasMethodAccess then UnauthorizedException () |> raise
      // initialize configured RestCreate handler
      let instance =
        tryGetService<IRestListCollection<'a>> serviceProvider
        |> Option.orElseWith  (fun () -> tryGetService<IRestListCollection> serviceProvider >>| Adapt.For<'a>)
        |> Option.defaultWith (fun () -> diActivate<DefaultRestListCollection<'a>> serviceProvider :> _)
      // create access validator
      let accessValidator =
        match access.List with
        | :? IQueryAccessValidator as accessValidator -> fun queryable -> accessValidator.AsyncFilterQuery (queryable, serviceProvider, httpContext.User)
        | _                                           -> async.Return
      // invoke method
      let! (struct (items, total)) =
        let boxed =
          match instance with
          | :? IBoxedInvoke<RestQuery, Linq.IQueryable -> Async<Linq.IQueryable>, struct ('a[] * int)> as boxed -> boxed
          | _  -> { new IBoxedInvoke<_, _, struct ('a[] * int)> with
                      member __.Instance = box instance
                      member __.AsyncInvoke (arg1, arg2) = instance.AsyncInvoke (arg1, arg2)
                  }
        RestMethodInvocation<'a, _, _, _> (boxed, parameters.RestQuery, accessValidator)
        |> restMethodInvoker.AsyncInvoke
      // set total header
      setResponseHeader "X-Total-Count" (total.ToString ()) httpContext
      // initialize configured serializer
      let serializer =
        tryGetService<ISerializer<'a[]>> serviceProvider
        |> Option.orElseWith  (fun () -> tryGetService<ISerializer> serviceProvider >>| Adapt.For<'a[]>)
        |> Option.defaultWith (fun () -> diActivate<DefaultSerializer<'a[]>> serviceProvider :> _)
      // serialize output
      let output = HttpContext.response httpContext |> HttpResponseOutput
      do! serializer.AsyncSerialize (output, items) }

  type private IInvoker =
    abstract Invoke : httpContext:HttpContext * services:RestMethodServices * parameters:ListParameters -> Async<unit>

  [<Sealed>]
  type private Invoker<'a> () =
    interface IInvoker with
      member __.Invoke (httpContext, services, parameters) = list<'a> httpContext services parameters

  let private cache = ConcurrentDictionary<Type, IInvoker> ()

  [<CompiledName("Invoke")>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let invoke httpContext services (parameters : ListParameters) =
    let instance = cache.GetOrAdd (parameters.EntityType, fun ty -> typedefof<Invoker<_>>.MakeGenericType ty |> activate :?> IInvoker)
    instance.Invoke (httpContext, services, parameters)
