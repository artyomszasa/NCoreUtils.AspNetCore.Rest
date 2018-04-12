namespace NCoreUtils.AspNetCore.Rest

open NCoreUtils
open System.Threading
open System
open System.Threading.Tasks
open System.IO
open Microsoft.Extensions.DependencyInjection

[<AbstractClass>]
type MappingSerializer<'source, 'target> (serviceProvider : IServiceProvider) =

  abstract GetDefaultSerializer<'b> : serviceProvider:IServiceProvider -> ISerializer<'b>

  abstract AsyncMap : item:'source -> Async<'target>

  abstract AsyncSerialize<'a> : stream:IConfigurableOutput<Stream> * item:'a -> Async<unit>

  default __.GetDefaultSerializer<'b> serviceProvider =
    tryGetService<ISerializer<'b>> serviceProvider
    |> Option.orElseWith (fun () -> tryGetService<ISerializer> serviceProvider |> Option.map Adapt.For<'b>)
    |> Option.defaultWith (fun () -> ActivatorUtilities.CreateInstance<DefaultSerializer<'b>> serviceProvider :> _)

  default this.AsyncSerialize<'a> (stream, item) = async {
    match box item with
    | :? 'source as source ->
      let! mapped = this.AsyncMap source
      let  serializer = this.GetDefaultSerializer<'target> serviceProvider
      do! serializer.AsyncSerialize (stream, mapped)
    | :? seq<'source> as sources ->
      let! mapped =
        sources
        |> Seq.map this.AsyncMap
        |> Seq.toArray
        |> Async.Sequential
      let  serializer = this.GetDefaultSerializer<'target[]> serviceProvider
      do! serializer.AsyncSerialize (stream, mapped)
    | _ ->
      let  serializer = this.GetDefaultSerializer<'a> serviceProvider
      do! serializer.AsyncSerialize (stream, item) }

  interface ISerializer with
    member this.AsyncSerialize<'a> (stream, item) = this.AsyncSerialize<'a> (stream, item)
  interface ISerializer<'source> with
    member this.AsyncSerialize (stream, item) = this.AsyncSerialize<'source> (stream, item)

[<AbstractClass>]
type AsyncMappingSerializer<'source, 'target> (serviceProvider) =
  inherit MappingSerializer<'source, 'target> (serviceProvider)
  abstract MapAsync : item:'source * cancellationToken:CancellationToken -> Task<'target>
  abstract SerializeAsync<'a> : stream:IConfigurableOutput<Stream> * item:'a * cancellationToken:CancellationToken -> Task

  member private __.BaseAsyncSerialize<'a> (stream, item) = base.AsyncSerialize<'a> (stream, item)

  override this.AsyncSerialize<'a> (stream, item : 'a) = Async.Adapt (fun cancellationToken -> this.SerializeAsync<'a> (stream, item, cancellationToken))

  default this.SerializeAsync<'a> (stream, item, cancellationToken) =
    Async.StartAsTask (this.BaseAsyncSerialize<'a> (stream, item), cancellationToken = cancellationToken) :> _

  default this.AsyncMap item = Async.Adapt (fun cancellationToken -> this.MapAsync (item, cancellationToken))
