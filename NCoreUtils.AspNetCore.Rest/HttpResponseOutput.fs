namespace NCoreUtils.AspNetCore.Rest

open System
open System.IO
open Microsoft.AspNetCore.Http


type internal HttpResponseOutput (response : HttpResponse) =
  interface IConfigurableOutput<Stream> with
    member __.AsyncInitialize info =
      if info.Length.HasValue then
        response.ContentLength <- info.Length
      if not (String.IsNullOrEmpty info.ContentType) then
        response.ContentType <- info.ContentType
      async.Return response.Body
