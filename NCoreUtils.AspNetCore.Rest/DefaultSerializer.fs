namespace NCoreUtils.AspNetCore.Rest

open Newtonsoft.Json
open System
open NCoreUtils
open System.ComponentModel
open System.Text
open System.IO
open System.Runtime.CompilerServices

type EncodingTypeConverter () =
  inherit TypeConverter ()
  override __.CanConvertFrom (_context, sourceType) = sourceType = typeof<string>
  override __.CanConvertTo (_context, destinationType) = destinationType = typeof<Encoding>
  override __.ConvertFrom (_context, _culture, value) =
    match value with
    | null                    -> null
    | :? Encoding as encoding -> encoding :> _
    | :? string   as name     -> Encoding.GetEncoding name :> _
    | _                       -> invalidOpf "Unable to convert object of type %A to %A" (value.GetType ()) typeof<Encoding>
  override __.ConvertTo (_context, _culture, value, destinationType) =
    if destinationType <> typeof<string> then invalidOpf "Should never happen."
    match value with
    | null -> null
    | :? Encoding as encoding -> encoding.WebName :> _
    | :? string   as name     -> name :> _
    | _                       -> invalidOpf "Should never happen."

/// Provides configuration for default serializer implementation.
type DefaultSerializerConfiguration () =
  /// Gets or sets buffer size.
  member val BufferSize = 8192              with get, set
  /// Gets or sets whether buffered serialization shpuld be used. Defaults to <c>false</c>.
  member val IsBuffered = false             with get, set
  /// Gets or sets encoding to use during serialization.
  [<TypeConverter(typeof<EncodingTypeConverter>)>]
  member val Encoding   = null : Encoding   with get, set
  /// <summary>
  /// Provides string representation of the current instance.
  /// </summary>
  /// <returns>String representation of the current instance.</returns>
  override this.ToString () =
    sprintf "[BufferSize = %d, IsBuffered = %A, Encoding = %A]"
      this.BufferSize
      this.IsBuffered
      (if isNull this.Encoding then null else this.Encoding.WebName)

/// Provides configuration for default serializer implementation scoped to a single type.
type DefaultSerializerConfiguration<'a> () =
  inherit DefaultSerializerConfiguration ()

[<AutoOpen>]
module private DefaultSerializerHelpers =
  [<CompiledName("Utf8")>]
  let utf8 = UTF8Encoding false :> Encoding

/// Provides default object serializer.
type DefaultSerializer<'a> =
  val private configuration : DefaultSerializerConfiguration
  val private jsonSettings : JsonSerializerSettings

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  /// <summary>
  /// Initializes new instance from the specified parameters.
  /// </summary>
  /// <param name="serviceProvider">Service provider.</param>
  new (serviceProvider : IServiceProvider) =
    let configuration =
      tryGetService<DefaultSerializerConfiguration<'a>> serviceProvider
      |> Option.map        (fun conf -> conf :> DefaultSerializerConfiguration)
      |> Option.orElseWith (fun ()   -> tryGetService<DefaultSerializerConfiguration> serviceProvider)
      |> Option.defaultWith DefaultSerializerConfiguration
    let jsonSettings =
      match tryGetService<JsonSerializerSettings> serviceProvider with
      | Some settings -> settings
      | None          -> JsonSerializerSettings (ReferenceLoopHandling = ReferenceLoopHandling.Ignore)
    { configuration = configuration
      jsonSettings  = jsonSettings }

  abstract AsyncSerialize : stream:IConfigurableOutput<Stream> * item:'a -> Async<unit>
  abstract AsyncBufferedSerialize : output:IConfigurableOutput<Stream> * item:'a -> Async<unit>
  abstract AsyncNonBufferedSerialize : output:IConfigurableOutput<Stream> * item:'a -> Async<unit>

  /// <summary>
  /// Serializes object of the specified type to configurable output using intermediate buffer.
  /// </summary>
  /// <typeparam name="a">Type of the object to serialize.</typeparam>
  /// <param name="stream">Configurable stream output to serialize to.</param>
  /// <param name="item">Object to serialize to.</param>
  default this.AsyncBufferedSerialize (output : IConfigurableOutput<Stream>, item : 'a) = async {
    let encoding = this.configuration.Encoding |?? utf8
    let data =
      JsonConvert.SerializeObject (item, this.jsonSettings)
      |> encoding.GetBytes
    let contentType = sprintf "application/json; charset=%s" encoding.HeaderName
    let! stream = output.AsyncInitialize { Length = Nullable.mk data.LongLength; ContentType = contentType }
    do! stream.AsyncWrite data
    do! stream.AsyncFlush () }

  /// <summary>
  /// Serializes object of the specified type to configurable output directly into output stream.
  /// </summary>
  /// <typeparam name="a">Type of the object to serialize.</typeparam>
  /// <param name="stream">Configurable stream output to serialize to.</param>
  /// <param name="item">Object to serialize to.</param>
  default this.AsyncNonBufferedSerialize (output : IConfigurableOutput<Stream>, item : 'a) = async {
    let encoding = this.configuration.Encoding |?? utf8
    let contentType = sprintf "application/json; charset=%s" encoding.HeaderName
    let! stream = output.AsyncInitialize { Length = Nullable.empty; ContentType = contentType }
    use  writer = new StreamWriter (stream, encoding, this.configuration.BufferSize, leaveOpen = true)
    let jsonSerializer = JsonSerializer.Create this.jsonSettings
    jsonSerializer.Serialize (writer, item) }

  /// <summary>
  /// Serializes object of the specified type to configurable output.
  /// </summary>
  /// <typeparam name="a">Type of the object to serialize.</typeparam>
  /// <param name="stream">Configurable stream output to serialize to.</param>
  /// <param name="item">Object to serialize to.</param>
  default this.AsyncSerialize (output, item) =
    match this.configuration.IsBuffered with
    | true -> this.AsyncBufferedSerialize    (output, item)
    | _    -> this.AsyncNonBufferedSerialize (output, item)


  interface ISerializer<'a> with
    member this.AsyncSerialize (stream, item) = this.AsyncSerialize (stream, item)
