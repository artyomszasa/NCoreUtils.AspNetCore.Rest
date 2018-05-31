namespace NCoreUtils.AspNetCore.Rest

open System.IO
open System.Text
open System.Runtime.CompilerServices
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open NCoreUtils
open NCoreUtils.AspNetCore
open NCoreUtils.Logging
open Newtonsoft.Json
open Newtonsoft.Json.Serialization

[<AutoOpen>]
module private DefaultDeserializerHelpers =
  [<CompiledName("Utf8")>]
  let utf8 = UTF8Encoding false :> Encoding

  let inline (|HasValue|_|) (stringSegment : Microsoft.Extensions.Primitives.StringSegment) =
    match stringSegment.HasValue with
    | true -> Some stringSegment.Value
    | _    -> None

/// Provides default object deserializer.
type DefaultDeserializer<'a> =
  val private httpContextAccessor : IHttpContextAccessor
  val private logger : ILogger<DefaultDeserializer<'a>>

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  /// <summary>
  /// Initializes new instance from the specified parameters.
  /// </summary>
  /// <param name="repository">Repository to use.</param>
  /// <param name="logger">Logger to use.</param>
  new (httpContextAccessor, logger) =
    { httpContextAccessor = httpContextAccessor
      logger              = logger }

  abstract Deserialize : stream:Stream -> 'a

  /// <summary>
  /// Deserializes object of the specified type with respest to the Content-Type header of the implicit HttpContext.
  /// </summary>
  /// <typeparam name="a">Deserialized object type.</typeparam>
  /// <param name="stream">Stream to deserialize from.</param>
  /// <returns>Deserialized object.</returns>
  default this.Deserialize stream =
    let request = HttpContext.request this.httpContextAccessor.HttpContext
    let headers = request.GetTypedHeaders ()
    match headers.ContentType with
    | null -> UnsupportedMediaTypeException "Unable to deserialize entity as no content type has been specified in request." |> raise
    | contentType ->
    match contentType.MediaType with
    | HasValue mediaType ->
      match mediaType with
      | EQI "application/json"
      | EQI "text/json" ->
        try
          let encoding = contentType.Encoding |?? utf8
          let settings =
            tryGetService<JsonSerializerSettings> this.httpContextAccessor.HttpContext.RequestServices
            |> Option.defaultWith (fun () -> JsonSerializerSettings (ContractResolver = CamelCasePropertyNamesContractResolver ()))
          let serializer = JsonSerializer.Create settings
          use reader = new StreamReader (stream, encoding, true, 8192, true)
          use jsonReader = new JsonTextReader (reader)
          let data = serializer.Deserialize<'a> jsonReader
          debugf this.logger "Successfully deserialized request body as %s" typeof<'a>.FullName
          data
        with exn ->
          BadRequestException (sprintf "Unable to deserialize request body as %s" typeof<'a>.FullName, exn) |> raise
      | _ -> UnsupportedMediaTypeException () |> raise
    | _ -> UnsupportedMediaTypeException "Unable to deserialize entity as no media type has been specified in request." |> raise

  interface IDeserializer<'a> with
    member this.Deserialize stream = this.Deserialize stream