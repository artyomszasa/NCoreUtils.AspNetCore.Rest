namespace NCoreUtils.AspNetCore.Rest

open System
open System.Collections.Concurrent
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open NCoreUtils
open NCoreUtils.AspNetCore
open System.Runtime.CompilerServices

// **************************************************************************
// LIST COLLECTION

type ListParameters = {
  [<ParameterBinder(typeof<ManagedTypeBinder>)>]
  EntityType : Type
  [<ParameterName("")>]
  RestQuery  : RestQuery }

type ListServices = IServiceProvider

module internal ListInvoker =

  [<RequiresExplicitTypeArguments>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let private list<'a> httpContext (services : ListServices) (parameters : ListParameters) = async {
    let instance =
      tryGetService<IRestListCollection<'a>> services
      |> Option.orElseWith  (fun () -> tryGetService<IRestListCollection> services >>| (fun selector -> selector.For<'a>()))
      |> Option.defaultWith (fun () -> ActivatorUtilities.CreateInstance<DefaultRestListCollection<'a>> services :> _)
    let! (struct (items, total)) = instance.AsyncInvoke parameters.RestQuery
    setResponseHeader "X-Total-Count" (total.ToString ()) httpContext
    let serializer =
      tryGetService<ISerializer<'a[]>> services
      |> Option.orElseWith  (fun () -> tryGetService<ISerializer> services >>| Adapt.For<'a[]>)
      |> Option.defaultWith (fun () -> ActivatorUtilities.CreateInstance<DefaultSerializer<'a[]>> services :> _)
    let output = HttpContext.response httpContext |> HttpResponseOutput
    do! serializer.AsyncSerialize (output, items) }


  type private IInvoker =
    abstract Invoke : httpContext:HttpContext * services:ListServices * parameters:ListParameters -> Async<unit>

  type private Invoker<'a> () =
    interface IInvoker with
      member __.Invoke (httpContext, services, parameters) = list<'a> httpContext services parameters

  let private cache = ConcurrentDictionary<Type, IInvoker> ()

  let invoke httpContext services (parameters : ListParameters) =
    let instance = cache.GetOrAdd (parameters.EntityType, fun ty -> typedefof<Invoker<_>>.MakeGenericType ty |> activate :?> IInvoker)
    instance.Invoke (httpContext, services, parameters)
