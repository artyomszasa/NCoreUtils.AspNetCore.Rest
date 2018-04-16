module NCoreUtils.AspNetCore.Rest.DefaultImplementationTests

open System
open System.Security.Claims
open Xunit
open Moq
open Microsoft.AspNetCore.Http
open NCoreUtils
open System.Collections.Immutable
open System.IO
open Microsoft.Extensions.Primitives
open System.Text
open Newtonsoft.Json
open NCoreUtils.Data
open System.Linq
open System.Linq.Expressions
open NCoreUtils.Linq
open System.Threading.Tasks
open System.Threading

type EnumerableAsyncQueryProvider () =
  // static let (|QVal|) (expr : Expression) =
  //   match expr with
  //   | ConstantExpression cexpr ->
  //     match xep

  interface IAsyncQueryProvider with

    member __.ExecuteAsync<'a> expression =
      let mutable q = Unchecked.defaultof<_>
      if not (expression.TryExtractQueryable (&q)) then
        invalidOp "Should never happen"
      // let pty = q.Provider.GetType ()
      // match pty.IsConstructedGenericType && pty.GetGenericTypeDefinition () = typedefof<IQ>
      // invalidOpf "%A" pty
      q.Provider.Execute<seq<'a>>(expression).ToAsyncEnumerable ()

    member __.ExecuteAsync<'a> (expression, _cancellationToken) =
      let mutable q = Unchecked.defaultof<_>
      if not (expression.TryExtractQueryable (&q)) then
        invalidOp "Should never happen"
      // let pty = q.Provider.GetType ()
      // // match pty.IsConstructedGenericType && pty.GetGenericTypeDefinition () = typedefof<IQ>
      // invalidOpf "%A" pty
      q.Provider.Execute<'a> expression |> Task.FromResult

type EnumerableQueryAdapter () =
  interface IAsyncQueryAdapter with
    member __.GetAdapterAsync (next, provider, _cancellationToken) =
      let pty = provider.GetType ()
      match pty.IsConstructedGenericType && pty.GetGenericTypeDefinition () = typedefof<System.Linq.EnumerableQuery<_>> with
      | true -> EnumerableAsyncQueryProvider () |> Task.FromResult<IAsyncQueryProvider>
      | _    -> next.Invoke ()

do AsyncQueryAdapters.Add <| EnumerableQueryAdapter ()

type XObject = {
    mutable Id : int }
    with
      interface IHasId<int> with member this.Id = this.Id

let private restConfiguration =
  { PathPrefix = []
    ManagedTypes = Map.add (CaseInsensitive "xobject") typeof<XObject> Map.empty
    AccessConfiguration = RestAccessConfiguration.allowAll }

let private ss v = (StringValues : string -> _) v

let private mockResponse () =
  let mock = Mock<HttpResponse>()
  let buffer = new MemoryStream ()
  let headers = HeaderDictionary ()
  let statusCode = ref 200
  mock.SetupGet(fun x -> x.Body).Returns buffer |> ignore
  mock.SetupGet(fun x -> x.Headers).Returns headers |> ignore
  mock.SetupSet(fun x -> x.ContentType).Callback(fun v -> headers.["Content-Type"] <- ss v)
  mock.SetupSet(fun x -> x.ContentLength).Callback(fun v -> if v.HasValue then headers.["Content-Length"] <- ss (v.Value.ToString()) else headers.Remove "Content-Length" |> ignore)
  mock.SetupSet(fun x -> x.StatusCode).Callback(Action<_> ((:=) statusCode))
  mock.Object, buffer, headers, statusCode

let private mockRequest () =
  let mock = Mock<HttpRequest>()
  let headers = HeaderDictionary ()
  headers.Add ("Accept", ss "application/json")
  mock.Object


[<Fact>]
let ``LIST method`` () =
  // mock service context
  let item = { Id = 1 }
  let items = [| item |] |> Queryable.AsQueryable
  let repositoryMock = Mock<IDataRepository<XObject>>()
  repositoryMock.SetupGet(fun x -> x.Items).Returns items |> ignore
  let repository = repositoryMock.Object
  let ctx = Common.MockHttpContext (services = addInstance repository)
  // mock parameters
  let restServices =
    { ServiceProvider   = ctx.HttpContext.RequestServices
      CurrentTypeName   = { Value = CaseInsensitive "xobject" }
      RestConfiguration = restConfiguration}
  let listParameters =
    { EntityType = typeof<XObject>
      RestQuery =
        { Offset          = 0
          Count           = 20
          Filter          = null
          SortBy          = ImmutableArray.Empty
          SortByDirection = ImmutableArray.Empty } }
  // invoke function
  ListInvoker.invoke ctx.HttpContext restServices listParameters |> Async.RunSynchronously
  // perform asserts
  Assert.Equal (200, ctx.ResponseStatusCode ())
  let json =
    let data = ctx.ResponseData ()
    use stream = new MemoryStream (data, false)
    use reader = new StreamReader (stream, Encoding.UTF8)
    reader.ReadToEnd ()
  let results = JsonConvert.DeserializeObject<XObject[]> json
  Assert.NotNull results
  Assert.Equal (1, results.Length)
  Assert.Equal ({ Id = 1 }, results.[0])
  Assert.True(true)

[<Fact>]
let ``ITEM method`` () =
  // mock service context
  let item = { Id = 1 }
  let items = [| item |] |> Queryable.AsQueryable
  let repositoryMock = Mock<IDataRepository<XObject, int>>()
  repositoryMock.SetupGet(fun x -> x.Items).Returns items |> ignore
  repositoryMock.Setup(fun x -> x.LookupAsync(1, It.IsAny<CancellationToken> ())).Returns (Task.FromResult item) |> ignore
  let repository = repositoryMock.Object
  let ctx = Common.MockHttpContext (services = addInstance repository)
  // mock parameters
  let restServices =
    { ServiceProvider   = ctx.HttpContext.RequestServices
      CurrentTypeName   = { Value = CaseInsensitive "xobject" }
      RestConfiguration = restConfiguration}
  let (itemParameters : ItemParameters) = { EntityType = typeof<XObject> }
  // invoke function
  ItemInvoker.invoke "1" ctx.HttpContext restServices itemParameters |> Async.RunSynchronously
  // perform assertions
  Assert.Equal (200, ctx.ResponseStatusCode ())
  let json =
    let data = ctx.ResponseData ()
    use stream = new MemoryStream (data, false)
    use reader = new StreamReader (stream, Encoding.UTF8)
    reader.ReadToEnd ()
  let result = JsonConvert.DeserializeObject<XObject> json
  Assert.NotNull result
  Assert.Equal ({ Id = 1 }, result)
  Assert.True(true)