namespace NCoreUtils.AspNetCore.Rest

open System
open System.IO
open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.DependencyInjection
open NCoreUtils

/// <summary>
/// Represents serializer that applies conversion/mapping on entities prior to serialization.
/// </summary>
[<AbstractClass>]
type MappingSerializer<'source, 'target> =
  val private serviceProvider : IServiceProvider

  /// <summary>
  /// Initializes new instance from the specified parameters.
  /// </summary>
  /// <param name="serviceProvider">Service provider.</param>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  new (serviceProvider) = { serviceProvider = serviceProvider }

  abstract GetDefaultSerializer<'b> : serviceProvider:IServiceProvider -> ISerializer<'b>

  /// <summary>
  /// Used for mapping entities.
  /// </summary>
  /// <param name="item">Source entity.</param>
  /// <returns>Mapped object.</returns>
  abstract AsyncMap : item:'source -> Async<'target>

  abstract AsyncSerialize<'a> : stream:IConfigurableOutput<Stream> * item:'a -> Async<unit>

  /// <summary>
  /// Resolves default serializer to serialize mapped entity. NOTE: this may result in twin recursion in overridden
  /// serializers.
  /// </summary>
  /// <param name="serviceProvider">Service provider used to resolve serializer for mapped object.</param>
  default __.GetDefaultSerializer<'b> serviceProvider =
    tryGetService<ISerializer<'b>> serviceProvider
    |> Option.orElseWith (fun () -> tryGetService<ISerializer> serviceProvider |> Option.map Adapt.For<'b>)
    |> Option.defaultWith (fun () -> ActivatorUtilities.CreateInstance<DefaultSerializer<'b>> serviceProvider :> _)

  /// <summary>
  /// Serializes generic entity. If entity or collection of entities can be mapped using the acutal instance, then
  /// mapping is performed and the result is serialized. Otherwise input is serialized using serializer registered for
  /// the input.
  /// </summary>
  /// <param name="output">Configurable output to use.</param>
  /// <param name="item">Item to serialize.</param>
  default this.AsyncSerialize<'a> (output, item) = async {
    match box item with
    | :? 'source as source ->
      let! mapped = this.AsyncMap source
      let  serializer = this.GetDefaultSerializer<'target> this.serviceProvider
      do! serializer.AsyncSerialize (output, mapped)
    | :? seq<'source> as sources ->
      let! mapped =
        sources
        |> Seq.map this.AsyncMap
        |> Async.Sequential
      let  serializer = this.GetDefaultSerializer<'target[]> this.serviceProvider
      do! serializer.AsyncSerialize (output, mapped)
    | _ ->
      let  serializer = this.GetDefaultSerializer<'a> this.serviceProvider
      do! serializer.AsyncSerialize (output, item) }

  interface ISerializer with
    member this.AsyncSerialize<'a> (stream, item) = this.AsyncSerialize<'a> (stream, item)
  interface ISerializer<'source> with
    member this.AsyncSerialize (stream, item) = this.AsyncSerialize<'source> (stream, item)

/// <summary>
/// Represents serializer that applies conversion/mapping on entities prior to serialization.
/// </summary>
[<AbstractClass>]
type AsyncMappingSerializer<'source, 'target> =
  inherit MappingSerializer<'source, 'target>

  /// <summary>
  /// Initializes new instance from the specified parameters.
  /// </summary>
  /// <param name="serviceProvider">Service provider.</param>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  new  (serviceProvider) = { inherit MappingSerializer<'source, 'target> (serviceProvider) }

  /// <summary>
  /// Used for mapping entities.
  /// </summary>
  /// <param name="item">Source entity.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Mapped object.</returns>
  abstract MapAsync : item:'source * cancellationToken:CancellationToken -> Task<'target>
  abstract SerializeAsync<'a> : stream:IConfigurableOutput<Stream> * item:'a * cancellationToken:CancellationToken -> Task

  member private __.BaseAsyncSerialize<'a> (stream, item) = base.AsyncSerialize<'a> (stream, item)

  override this.AsyncSerialize<'a> (stream, item : 'a) = Async.Adapt (fun cancellationToken -> this.SerializeAsync<'a> (stream, item, cancellationToken))

  /// <summary>
  /// Serializes generic entity. If entity or collection of entities can be mapped using the acutal instance, then
  /// mapping is performed and the result is serialized. Otherwise input is serialized using serializer registered for
  /// the input.
  /// </summary>
  /// <param name="output">Configurable output to use.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <param name="item">Item to serialize.</param>
  default this.SerializeAsync<'a> (stream, item, cancellationToken) =
    Async.StartAsTask (this.BaseAsyncSerialize<'a> (stream, item), cancellationToken = cancellationToken) :> _

  default this.AsyncMap item = Async.Adapt (fun cancellationToken -> this.MapAsync (item, cancellationToken))
