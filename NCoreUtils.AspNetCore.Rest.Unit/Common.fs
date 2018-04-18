namespace NCoreUtils.AspNetCore.Rest

open System
open Moq
open Microsoft.AspNetCore.Http
open System.IO
open Microsoft.Extensions.Primitives
open System.Security.Claims
open Microsoft.Extensions.Logging
open System.Reflection
open NCoreUtils

// type ServiceMocker (mock : Mock<IServiceProvider>) =
//   member Add<'service>(service : 'service) = mock.Setup(fun x -> x.GetService(typeof<'service>)).Returns service
//   member Add<'service>(service : unit -> 'service) =
//     mock.Setup(fun x -> x.GetService(typeof<'service>)).Returns (Func<_> service)

[<AutoOpen>]
module ServiceMocker =

  type Services =
    internal
    | Services of (Type -> IServiceProvider -> obj option) list

  let noServices = Services []

  let addInstance (service : 'service) (Services svcs) =
    (fun ty _ -> if ty = typeof<'service> then Some (box service) else None) :: svcs
    |> Services


  let addFactory (service : unit -> 'service) (Services svcs) =
    (fun ty _ -> if ty = typeof<'service> then Some (service () |> box) else None) :: svcs
    |> Services

  let addServiceFactory (service : IServiceProvider -> 'service) (Services svcs) =
    (fun ty sp -> if ty = typeof<'service> then Some (service sp |> box) else None) :: svcs
    |> Services

  let addServiceFactoryEx (service : Type -> IServiceProvider -> 'service option) (Services svcs) =
    (fun ty sp -> service ty sp >>| box) :: svcs
    |> Services

  let getMockService (spref : IServiceProvider ref) (Services svcs) ty =
    let res =
      svcs
      |> List.tryPick (fun svc -> svc ty spref.Value)
      |> Option.getOrDef Unchecked.defaultof<_>
    // printfn "Service request for %A => %A" ty res
    res

type MockedHttpContext = {
  HttpContext        : HttpContext
  ResponseHeaders    : IHeaderDictionary
  ResponseData       : unit -> byte[]
  ResponseStatusCode : unit -> int }

type Common private () =

  static let ss v = (StringValues : string -> _) v

  static let mockResponse (initResponse : (Mock<HttpResponse> -> unit) option) =
    let mock = Mock<HttpResponse>()
    let buffer = new MemoryStream ()
    let headers = HeaderDictionary ()
    let statusCode = ref 200
    mock.SetupGet(fun x -> x.Body).Returns buffer |> ignore
    mock.SetupGet(fun x -> x.Headers).Returns headers |> ignore
    mock.SetupSet(fun x -> x.ContentType).Callback(fun v -> headers.["Content-Type"] <- ss v) |> ignore
    mock.SetupSet(fun x -> x.ContentLength).Callback(fun v -> if v.HasValue then headers.["Content-Length"] <- ss (v.Value.ToString()) else headers.Remove "Content-Length" |> ignore) |> ignore
    mock.SetupSet(fun x -> x.StatusCode).Callback(Action<_> ((:=) statusCode)) |> ignore
    match initResponse with
    | Some initResponse -> initResponse mock
    | _                 -> ()
    mock.Object, buffer, headers, statusCode

  static let mockRequest (initRequest : (Mock<HttpRequest> -> unit) option) =
    let mock = Mock<HttpRequest>()
    let headers = HeaderDictionary ()
    headers.Add ("Accept", ss "application/json")
    match initRequest with
    | Some initRequest -> initRequest mock
    | _                -> ()
    mock.Object

  static member MkTypedLogger<'a> (logger : ILogger) =
    { new ILogger<'a> with
        member __.IsEnabled logLevel = logger.IsEnabled logLevel
        member __.Log (logLevel, eventId, state, exn, formatter) = logger.Log (logLevel, eventId, state, exn, formatter)
        member __.BeginScope state = logger.BeginScope state
    }

  static member MockHttpContext (?services : Services -> Services,
                                 ?initRequest  : Mock<HttpRequest>  -> unit,
                                 ?initResponse : Mock<HttpResponse> -> unit,
                                 ?initUser     : unit -> ClaimsPrincipal,
                                 ?initContext  : Mock<HttpContext> -> unit) =
    let httpContextRef = ref (null : HttpContext)
    let httpContextAccessor =
      { new IHttpContextAccessor with
        member __.HttpContext = httpContextRef.Value
        member __.HttpContext with set v = httpContextRef := v
      }

    let createLogger (_categoryName : string) =
      { new ILogger with
          member __.IsEnabled _ = true
          member __.Log (_, _, _, _, _) = ()
          member __.BeginScope _ = { new IDisposable with member __.Dispose () = () }
      }
    let loggerFactory =
      { new ILoggerFactory with
          member __.CreateLogger categoryName = createLogger categoryName
          member __.AddProvider _ = ()
          member __.Dispose () = ()
      }

    let handleLoggerService (ty : Type) =
      match ty.IsConstructedGenericType && ty.GetGenericTypeDefinition () = typedefof<ILogger<_>> with
      | true ->
        let gm = typeof<Common>.GetMethod ("MkTypedLogger", BindingFlags.Static ||| BindingFlags.Public ||| BindingFlags.NonPublic)
        let m  = gm.MakeGenericMethod ty.GenericTypeArguments.[0]
        Some (m.Invoke (null, [| loggerFactory.CreateLogger ty.FullName |]))
      | _ -> None

    let serviceProviderRef = ref (null : IServiceProvider)
    let services =
      let services0 =
        noServices
        |> addFactory (fun () -> serviceProviderRef.Value)
        |> addServiceFactoryEx (fun ty _ -> handleLoggerService ty)
        |> addInstance loggerFactory
        |> addInstance httpContextAccessor
      match services with
      | Some services -> services services0
      | _             -> services0
    let serviceProviderMock = Mock<IServiceProvider>()
    serviceProviderMock.Setup(fun x -> x.GetService(It.IsAny<Type> ())).Returns (Func<_, _> (fun ty -> getMockService serviceProviderRef services ty)) |> ignore
    let serviceProvider = serviceProviderMock.Object
    serviceProviderRef := serviceProvider
    let request = mockRequest initRequest
    let (response, buffer, headers, statusCode) = mockResponse initResponse
    let user =
      match initUser with
      | Some initUser -> initUser ()
      | _             -> ClaimsPrincipal ()
    let contextMock = Mock<HttpContext>()
    contextMock.SetupGet(fun x -> x.User).Returns user |> ignore
    contextMock.SetupGet(fun x -> x.RequestServices).Returns serviceProvider |> ignore
    contextMock.SetupGet(fun x -> x.Request).Returns request |> ignore
    contextMock.SetupGet(fun x -> x.Response).Returns response |> ignore
    match initContext with
    | Some initContext -> initContext contextMock
    | _                -> ()
    let context = contextMock.Object
    httpContextRef := context
    { HttpContext = context
      ResponseHeaders    = headers
      ResponseData       = fun () -> buffer.ToArray ()
      ResponseStatusCode = fun () -> statusCode.Value }


