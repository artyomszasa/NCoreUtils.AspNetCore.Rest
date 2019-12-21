module NCoreUtils.AspNetCore.Rest.DefaultImplementationTests

open System
open System.Collections.Immutable
open System.Linq
open System.IO
open System.Text
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Primitives
open Moq
open NCoreUtils
open NCoreUtils.Data
open Newtonsoft.Json
open Xunit

// *********************************************************
// IAsyncQueryProvider/AsyncQueryAdapter for test repository

type private AsAsyncEnumerator<'T> (source : Collections.Generic.IEnumerator<'T>) =
  interface Collections.Generic.IAsyncEnumerator<'T> with
    member __.Current = source.Current
    member __.DisposeAsync () =
      source.Dispose ()
      Unchecked.defaultof<_>
    member __.MoveNextAsync () =
      ValueTask<bool> (source.MoveNext ())

type private AsAsyncEnumerable<'T> (source : Collections.Generic.IEnumerable<'T>) =
  interface Collections.Generic.IAsyncEnumerable<'T> with
    member __.GetAsyncEnumerator _ = AsAsyncEnumerator<'T> (source.GetEnumerator ()) :> _


type EnumerableAsyncQueryProvider () =
  interface Linq.IAsyncQueryProvider with

    member __.ExecuteEnumerableAsync<'a> (expression) =
      let mutable q = Unchecked.defaultof<_>
      if not (expression.TryExtractQueryable (&q)) then
        invalidOp "Should never happen"
      AsAsyncEnumerable<_> (q.Provider.Execute<seq<'a>>(expression)) :> Collections.Generic.IAsyncEnumerable<_>

    member __.ExecuteAsync<'a> (expression, _cancellationToken) =
      let mutable q = Unchecked.defaultof<_>
      if not (expression.TryExtractQueryable (&q)) then
        invalidOp "Should never happen"
      q.Provider.Execute<'a> expression |> Task.FromResult

type EnumerableQueryAdapter () =
  interface Linq.IAsyncQueryAdapter with
    member __.GetAdapterAsync (next, provider, _cancellationToken) =
      let pty = provider.GetType ()
      match pty.IsConstructedGenericType && pty.GetGenericTypeDefinition () = typedefof<EnumerableQuery<_>> with
      | true -> EnumerableAsyncQueryProvider () |> ValueTask<Linq.IAsyncQueryProvider>
      | _    -> next.Invoke ()

do Linq.AsyncQueryAdapters.Add <| EnumerableQueryAdapter ()

// *********************************************************
// Test object
type XObject = { [<field:JsonIgnore>] mutable Id : int } with interface IHasId<int> with member this.Id = this.Id

// *********************************************************

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
  mock.SetupSet<string>(fun x -> x.ContentType <- It.IsAny<string> ())
    .Callback(fun v -> headers.["Content-Type"] <- ss v) |> ignore
  mock.SetupSet<Nullable<int64>>(fun x -> x.ContentLength <- It.IsAny<Nullable<int64>> ())
    .Callback(fun v -> if v.HasValue then headers.["Content-Length"] <- ss (v.Value.ToString()) else headers.Remove "Content-Length" |> ignore) |> ignore
  mock.SetupSet<int>(fun x -> x.StatusCode <- It.IsAny<int> ())
    .Callback(Action<_> ((:=) statusCode)) |> ignore
  mock.Object, buffer, headers, statusCode

let private mockRequest () =
  let mock = Mock<HttpRequest>()
  let headers = HeaderDictionary ()
  headers.Add ("Accept", ss "application/json")
  mock.Object

type DummyDataTransaction () =
  let guid = Guid.NewGuid ()
  interface IDataTransaction with
    member __.Guid = guid
    member __.Commit () = ()
    member __.Rollback () = ()
    member __.Dispose () = ()

type DummyDataContext () =
  interface IDataRepositoryContext with
    member __.CurrentTransaction = Unchecked.defaultof<_>
    member __.BeginTransactionAsync (_, _) = new DummyDataTransaction () |> Task.FromResult<IDataTransaction>
    member __.Dispose () = ()

[<Fact>]
let ``RestQuery binding`` () =
  let parameters =
    Map.ofList
      [ "X-Offset",            [ "10" ]
        "X-Count",             [ "10" ]
        "X-Filter",            [ "filter" ]
        "X-Sort-By",           [ "a,b" ]
        "X-Sort-By-Direction", [ "asc,desc" ] ]
  let noServices = ServiceCollection().BuildServiceProvider(true)
  let q =
    NCoreUtils.ParameterBinding.asyncBindNoAttrs<RestQuery> noServices (fun key -> Map.tryFind key parameters) null
    |> Async.RunSynchronously
  Assert.Equal (10, q.Offset)
  Assert.Equal (10, q.Count)
  Assert.Equal ("filter", q.Filter)
  Assert.Equal (2, q.SortBy.Length)
  Assert.Equal (2, q.SortByDirection.Length)
  Assert.Equal ("a", q.SortBy.[0])
  Assert.Equal ("b", q.SortBy.[1])
  Assert.Equal (RestSortByDirection.Asc, q.SortByDirection.[0])
  Assert.Equal (RestSortByDirection.Desc, q.SortByDirection.[1])

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
      RestConfiguration = restConfiguration
      RestMethodInvoker = DefaultRestMethodInvoker () }
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
      RestConfiguration = restConfiguration
      RestMethodInvoker = DefaultRestMethodInvoker () }
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

let private unwrapUnsafe x =
  match x with
  | Some x -> x
  | _      -> Unchecked.defaultof<_>

[<Fact>]
let ``CREATE method`` () =
  // mock service context
  let item = { Id = 1 }
  let items = ResizeArray [| |]
  let repositoryMock = Mock<IDataRepository<XObject, int>>()
  repositoryMock.SetupGet(fun x -> x.Items).Returns (Func<_> (fun _ -> items |> Queryable.AsQueryable)) |> ignore
  repositoryMock.SetupGet(fun x -> x.Context).Returns (Func<_> (fun _ -> new DummyDataContext () :> IDataRepositoryContext)) |> ignore
  repositoryMock.Setup(fun x -> x.LookupAsync(1, It.IsAny<CancellationToken> ())).Returns (Func<_, _, _> (fun id _ -> Seq.tryFind (fun x -> x.Id = id) items |> unwrapUnsafe |> Task.FromResult)) |> ignore
  repositoryMock.Setup(fun x -> x.PersistAsync(It.IsAny<XObject> (), It.IsAny<CancellationToken> ())).Returns (Func<_, _, _> (fun obj _ -> items.Add obj; Task.FromResult obj)) |> ignore
  let repository = repositoryMock.Object
  let initRequest (m : Mock<HttpRequest>) =
    let json = JsonConvert.SerializeObject item
    let enc = UTF8Encoding false
    let requestStream = new MemoryStream (enc.GetBytes json, false)
    m.SetupGet(fun x -> x.Body).Returns requestStream |> ignore
    let headers = HeaderDictionary ()
    headers.Add ("Content-Type", ss "application/json")
    m.SetupGet(fun x -> x.Headers).Returns headers |> ignore
    m.SetupGet(fun x -> x.Scheme).Returns "http" |> ignore
    m.SetupGet(fun x -> x.Host).Returns (HostString "rest") |> ignore
  let ctx = Common.MockHttpContext (services = addInstance repository, initRequest = initRequest)
  // mock parameters
  let restServices =
    { ServiceProvider   = ctx.HttpContext.RequestServices
      CurrentTypeName   = { Value = CaseInsensitive "xobject" }
      RestConfiguration = restConfiguration
      RestMethodInvoker = DefaultRestMethodInvoker () }
  let (createParameters : CreateParameters) = { EntityType = typeof<XObject> }
  // invoke function
  CreateInvoker.invoke ctx.HttpContext restServices createParameters |> Async.RunSynchronously
  // perform assertions
  Assert.Equal (201, ctx.ResponseStatusCode ())
  Assert.Equal (1, items.Count)
  let result = items.[0]
  Assert.NotNull result
  Assert.Equal ({ Id = 1 }, result)
  Assert.True (ctx.ResponseHeaders.ContainsKey "Location")
  let locationValues = ctx.ResponseHeaders.["Location"]
  Assert.Equal (1, locationValues.Count)
  Assert.Equal ("http://rest/xobject/1", locationValues.[0], ignoreCase = true)