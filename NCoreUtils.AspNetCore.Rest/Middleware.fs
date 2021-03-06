namespace NCoreUtils.AspNetCore

open System
open Microsoft.Extensions.Logging
open NCoreUtils
open NCoreUtils.AspNetCore
open NCoreUtils.AspNetCore.Rest
open NCoreUtils.Logging

[<RequireQualifiedAccess>]
module RestMiddleware =

  let rec private stripPrefix (prefix : CaseInsensitive list) path =
    match prefix, path with
    | [], _ -> ValueSome path
    | _, [] -> ValueNone
    | x :: prefix', y :: path' ->
    match x = y with
    | true -> stripPrefix prefix' path'
    | _    -> ValueNone

  let private mkHeaderParameterSource httpContext : ParameterSource =
    let headers = HttpContext.requestHeaders httpContext
    fun headerName ->
      let mutable values = Unchecked.defaultof<_>
      match headers.TryGetValue (headerName, &values) with
      | true ->
        values
        |> Seq.map (Uri.UnescapeDataString)
        |> List.ofSeq
        |> Some
      | _    -> None

  /// <summary>
  /// Executes REST processing pipeline.
  /// </summary>
  /// <param name="configuration">REST pipeline configuration.</param>
  /// <param name="httpContext">Current HttpContext.</param>
  /// <param name="asyncNext">A function that handles the request or calls the given next function.</param>
  [<CompiledName("Run")>]
  let run (configuration : RestConfiguration) httpContext asyncNext =
    match stripPrefix configuration.PathPrefix <| HttpContext.path httpContext with
    | ValueNone -> asyncNext
    | ValueSome path ->
      let logger = (getRequiredService<ILoggerFactory> httpContext.RequestServices).CreateLogger "NCoreUtils.AspNetCore.RestMiddleware"
      debugf logger "Probing path [%s]" (path |> Seq.map (fun ci -> ci.Value) |> String.concat ",")
      match path with
      | typeName :: routeArgs ->
        HttpContext.requestServices httpContext
          |> getRequiredService<CurrentRestTypeName>
          |> setCurrentRestTypeName typeName
        match routeArgs, HttpContext.httpMethod httpContext with
        | [],        HttpGet    -> HttpContext.asyncBindAndExecute httpContext (mkHeaderParameterSource httpContext)  ListInvoker.invoke
        | [],        HttpPost   -> HttpContext.asyncBindAndExecute httpContext (mkHeaderParameterSource httpContext)  CreateInvoker.invoke
        | [],        _          -> MethodNotAllowedException () |> raise
        | [ rawId ], HttpGet    -> HttpContext.asyncBindAndExecute httpContext (mkHeaderParameterSource httpContext) (ItemInvoker.invoke   rawId.Value)
        | [ rawId ], HttpPut    -> HttpContext.asyncBindAndExecute httpContext (mkHeaderParameterSource httpContext) (UpdateInvoker.invoke rawId.Value)
        | [ rawId ], HttpDelete -> HttpContext.asyncBindAndExecute httpContext (mkHeaderParameterSource httpContext) (DeleteInvoker.invoke rawId.Value)
        | [ _     ], _          -> MethodNotAllowedException () |> raise
        | _                     -> asyncNext
      | _ -> asyncNext

  /// <summary>
  /// Executes REST processing pipeline.
  /// </summary>
  /// <param name="configuration">REST pipeline configuration.</param>
  /// <param name="httpContext">Current HttpContext.</param>
  /// <param name="next">A function that handles the request or calls the given next function.</param>
  [<CompiledName("Run")>]
  let runAsTask configuration httpContext (next : System.Func<System.Threading.Tasks.Task>) =
    let asyncNext = Async.Adapt (fun _ -> next.Invoke ())
    Async.StartAsTask (run configuration httpContext asyncNext) :> System.Threading.Tasks.Task
