namespace NCoreUtils.AspNetCore

open Microsoft.Extensions.Logging
open NCoreUtils
open NCoreUtils.AspNetCore
open NCoreUtils.AspNetCore.Rest
open NCoreUtils.Logging

[<RequireQualifiedAccess>]
module RestMiddleware =

  let rec private stripPrefix (prefix : CaseInsensitive list) path =
    match prefix, path with
    | [], _ -> Some path
    | _, [] -> None
    | x :: prefix', y :: path' ->
    match x = y with
    | true -> stripPrefix prefix' path'
    | _    -> None

  let private mkHeaderParameterSource httpContext : ParameterSource =
    let headers = HttpContext.requestHeaders httpContext
    fun headerName ->
      let mutable values = Unchecked.defaultof<_>
      match headers.TryGetValue (headerName, &values) with
      | true -> Some <| List.ofSeq values
      | _    -> None

  /// <summary>
  /// Executes REST processing pipeline.
  /// </summary>
  /// <param name="configuration">REST pipeline configuration.</param>
  /// <param name="HttpContext">Current HttpContext.</param>
  /// <param name="asyncNext">A function that handles the request or calls the given next function.</param>
  [<CompiledName("Run")>]
  let run (configuration : RestConfiguration) httpContext asyncNext =
    match stripPrefix configuration.PathPrefix <| HttpContext.path httpContext with
    | None -> asyncNext
    | Some path ->
      let logger = (getRequiredService<ILoggerFactory> httpContext.RequestServices).CreateLogger "NCoreUtils.AspNetCore.RestMiddleware"
      debugf logger "Probing path [%s]" (path |> Seq.map (fun ci -> ci.Value) |> String.concat ",")
      match path with
      | typeName :: routeArgs ->
        HttpContext.requestServices httpContext
          |> getRequiredService<CurrentRestTypeName>
          |> setCurrentRestTypeName typeName
        match routeArgs, HttpContext.httpMethod httpContext with
        | [],        HttpMethod.HttpGet    -> HttpContext.asyncBindAndExecute httpContext (mkHeaderParameterSource httpContext)  ListInvoker.invoke
        | [],        HttpMethod.HttpPost   -> HttpContext.asyncBindAndExecute httpContext (mkHeaderParameterSource httpContext)  CreateInvoker.invoke
        | [],        _                     -> MethodNotAllowedException () |> raise
        | [ rawId ], HttpMethod.HttpGet    -> HttpContext.asyncBindAndExecute httpContext (mkHeaderParameterSource httpContext) (ItemInvoker.invoke   rawId.Value)
        | [ rawId ], HttpMethod.HttpPut    -> HttpContext.asyncBindAndExecute httpContext (mkHeaderParameterSource httpContext) (UpdateInvoker.invoke rawId.Value)
        | [ rawId ], HttpMethod.HttpDelete -> HttpContext.asyncBindAndExecute httpContext (mkHeaderParameterSource httpContext) (DeleteInvoker.invoke rawId.Value)
        | [ _     ], _                     -> MethodNotAllowedException () |> raise
        | _                                -> asyncNext
      | _ -> asyncNext