namespace NCoreUtils.AspNetCore.Rest

open System
open Moq
open Microsoft.AspNetCore.Http
open System.IO
open Microsoft.Extensions.Primitives
open System.Security.Claims

// type ServiceMocker (mock : Mock<IServiceProvider>) =
//   member Add<'service>(service : 'service) = mock.Setup(fun x -> x.GetService(typeof<'service>)).Returns service
//   member Add<'service>(service : unit -> 'service) =
//     mock.Setup(fun x -> x.GetService(typeof<'service>)).Returns (Func<_> service)

[<AutoOpen>]
module ServiceMocker =

  let addInstance (service : 'service) (mock : Mock<IServiceProvider>) =
    mock.Setup(fun x -> x.GetService(typeof<'service>)).Returns (Func<_> (fun () -> service)) |> ignore
    mock

  let addFactory (service : unit -> 'service) (mock : Mock<IServiceProvider>) =
    mock.Setup(fun x -> x.GetService(typeof<'service>)).Returns (Func<_> service) |> ignore
    mock


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
    mock.SetupSet(fun x -> x.ContentType).Callback(fun v -> headers.["Content-Type"] <- ss v)
    mock.SetupSet(fun x -> x.ContentLength).Callback(fun v -> if v.HasValue then headers.["Content-Length"] <- ss (v.Value.ToString()) else headers.Remove "Content-Length" |> ignore)
    mock.SetupSet(fun x -> x.StatusCode).Callback(Action<_> ((:=) statusCode))
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


  static member MockHttpContext (?services : Mock<IServiceProvider> -> Mock<IServiceProvider>,
                                 ?initRequest  : Mock<HttpRequest>  -> unit,
                                 ?initResponse : Mock<HttpResponse> -> unit,
                                 ?initUser     : unit -> ClaimsPrincipal,
                                 ?initContext  : Mock<HttpContext> -> unit) =
    let serviceProviderRef = ref (null : IServiceProvider)
    let serviceProviderMock = Mock<IServiceProvider>()
    serviceProviderMock.Setup(fun x -> x.GetService(typeof<IServiceProvider>)).Returns(Func<_> (fun _ -> serviceProviderRef.Value)) |> ignore
    match services with
    | Some services -> services serviceProviderMock |> ignore
    | _             -> ()
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
    { HttpContext = context
      ResponseHeaders    = headers
      ResponseData       = fun () -> buffer.ToArray ()
      ResponseStatusCode = fun () -> statusCode.Value }


