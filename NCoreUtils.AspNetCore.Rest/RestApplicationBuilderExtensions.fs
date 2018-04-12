namespace NCoreUtils.AspNetCore

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open System.Runtime.CompilerServices

[<Sealed>]
type internal RestInvoker (configuration : Rest.RestConfiguration, next : RequestDelegate) =
  inherit FSharpMiddleware (next)
  override __.AsyncInvoke (httpContext, asyncNext) =
    RestMiddleware.run configuration httpContext asyncNext

[<Sealed; AbstractClass>]
[<Extension>]
type RestApplicationBuilderExtensions =

  [<Extension>]
  static member UseRest (this : IApplicationBuilder, configuration : Rest.RestConfiguration) =
    this.Use(RestMiddleware.run configuration)

  [<Extension>]
  static member UseRest (this : IApplicationBuilder) =
    this.UseMiddleware<RestInvoker>()