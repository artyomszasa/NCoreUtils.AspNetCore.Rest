namespace NCoreUtils.AspNetCore.Rest

open Newtonsoft.Json
open System
open NCoreUtils
open System.ComponentModel
open System.Text
open System.IO
open System.Runtime.InteropServices

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

type DefaultSerializerConfiguration () =
  member val BufferSize = 8192              with get, set
  member val IsBuffered = false             with get, set
  [<TypeConverter(typeof<EncodingTypeConverter>)>]
  member val Encoding   = null : Encoding   with get, set
  override this.ToString () =
    sprintf "[BufferSize = %d, IsBuffered = %A, Encoding = %A]"
      this.BufferSize
      this.IsBuffered
      (if isNull this.Encoding then null else this.Encoding.WebName)

type DefaultSerializerConfiguration<'a> () =
  inherit DefaultSerializerConfiguration ()

type DefaultSerializer<'a> (serviceProvider : IServiceProvider) =
  static let utf8 = UTF8Encoding false :> Encoding

  let configuration =
    tryGetService<DefaultSerializerConfiguration<'a>> serviceProvider
    |> Option.map        (fun conf -> conf :> DefaultSerializerConfiguration)
    |> Option.orElseWith (fun ()   -> tryGetService<DefaultSerializerConfiguration> serviceProvider)
    |> Option.defaultWith DefaultSerializerConfiguration

  let jsonSettings =
    match tryGetService<JsonSerializerSettings> serviceProvider with
    | Some settings -> settings
    | None          -> JsonSerializerSettings (ReferenceLoopHandling = ReferenceLoopHandling.Ignore)

  abstract AsyncSerialize : stream:IConfigurableOutput<Stream> * item:'a -> Async<unit>
  abstract AsyncBufferedSerialize : output:IConfigurableOutput<Stream> * item:'a -> Async<unit>
  abstract AsyncNonBufferedSerialize : output:IConfigurableOutput<Stream> * item:'a -> Async<unit>

  default __.AsyncBufferedSerialize (output : IConfigurableOutput<Stream>, item : 'a) = async {
    let encoding = configuration.Encoding |?? utf8
    let data =
      JsonConvert.SerializeObject (item, jsonSettings)
      |> encoding.GetBytes
    let contentType = sprintf "application/json; charset=%s" encoding.HeaderName
    use! stream = output.AsyncInitialize { Length = Nullable.mk data.LongLength; ContentType = contentType }
    do! stream.AsyncWrite data }

  default __.AsyncNonBufferedSerialize (output : IConfigurableOutput<Stream>, item : 'a) = async {
    let encoding = configuration.Encoding |?? utf8
    let contentType = sprintf "application/json; charset=%s" encoding.HeaderName
    use! stream = output.AsyncInitialize { Length = Nullable.empty; ContentType = contentType }
    use  writer = new StreamWriter (stream, encoding, configuration.BufferSize, leaveOpen = true)
    let jsonSerializer = JsonSerializer.Create jsonSettings
    jsonSerializer.Serialize (writer, item) }

  default this.AsyncSerialize (stream, item) =
    match configuration.IsBuffered with
    | true -> this.AsyncBufferedSerialize    (stream, item)
    | _    -> this.AsyncNonBufferedSerialize (stream, item)


  interface ISerializer<'a> with
    member this.AsyncSerialize (stream, item) = this.AsyncSerialize (stream, item)
