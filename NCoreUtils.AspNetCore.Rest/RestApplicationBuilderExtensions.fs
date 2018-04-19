namespace NCoreUtils.AspNetCore

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open System.Runtime.CompilerServices

[<Sealed>]
type internal RestInvoker (configuration : Rest.RestConfiguration, next : RequestDelegate) =
  inherit FSharpMiddleware (next)
  override __.AsyncInvoke (httpContext, asyncNext) =
    RestMiddleware.run configuration httpContext asyncNext

/// Contains extensions for adding REST to ASP.NET core application pipeline.
[<Sealed; AbstractClass>]
[<Extension>]
type RestApplicationBuilderExtensions =

  /// <summary>
  /// Adds REST handler to application pipeline with the specified configuration.
  /// </summary>
  /// <param name="this">Application builder.</param>
  /// <param name="configuration">REST pipeline configuration.</param>
  /// <returns>Application builder for chaining.</returns>
  [<Extension>]
  static member UseRest (this : IApplicationBuilder, configuration : Rest.RestConfiguration) =
    this.Use(RestMiddleware.run configuration)

  /// <summary>
  /// Adds REST handler to application pipeline with configuration defined in dependency injection context.
  /// </summary>
  /// <param name="this">Application builder.</param>
  /// <returns>Application builder for chaining.</returns>
  [<Extension>]
  static member UseRest (this : IApplicationBuilder) =
    this.UseMiddleware<RestInvoker>()