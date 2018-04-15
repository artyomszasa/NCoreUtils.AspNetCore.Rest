namespace NCoreUtils.AspNetCore.Rest

open System.IO
open System.Text
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open NCoreUtils
open NCoreUtils.AspNetCore
open NCoreUtils.Logging
open Newtonsoft.Json

type DefaultDeserializer<'a> (httpContextAccessor : IHttpContextAccessor, logger : ILogger<DefaultDeserializer<'a>>) =

  static let utf8 = UTF8Encoding false :> Encoding

  static let (|HasValue|_|) (stringSegment : Microsoft.Extensions.Primitives.StringSegment) =
    match stringSegment.HasValue with
    | true -> Some stringSegment.Value
    | _    -> None

  interface IDeserializer<'a> with
    member __.Deserialize stream =
      let request = HttpContext.request httpContextAccessor.HttpContext
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
            let serializer = new JsonSerializer ()
            use reader = new StreamReader (stream, encoding, true, 8192, true)
            use jsonReader = new JsonTextReader (reader)
            let data = serializer.Deserialize<'a> jsonReader
            debugf logger "Successfully deserialized request body as %s" typeof<'a>.FullName
            data
          with exn ->
            BadRequestException (sprintf "Unable to deserialize request body as %s" typeof<'a>.FullName, exn) |> raise
        | _ -> UnsupportedMediaTypeException () |> raise
      | _ -> UnsupportedMediaTypeException "Unable to deserialize entity as no media type has been specified in request." |> raise