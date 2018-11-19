namespace NCoreUtils.AspNetCore

open System
open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection.Extensions
open NCoreUtils.AspNetCore.Rest

/// Contains extensions for adding REST reslated services to the container.
[<Sealed; AbstractClass>]
[<Extension>]
type RestServiceCollectionExtensions =

  /// <summary>
  /// Configures container based REST configuration.
  /// <summary>
  /// <param name="this">Service container.</param>
  /// <param name="configure">REST configuration function.</param>
  /// <returns>Service container reference for chaining.</returns>
  [<Extension>]
  static member ConfigureRest (this : IServiceCollection, configure : Action<Rest.RestConfigurationBuilder>) =
    let builder = Rest.RestConfigurationBuilder ()
    configure.Invoke builder
    this.TryAddSingleton<IRestMethodInvoker, DefaultRestMethodInvoker> ()
    this.TryAddScoped<CurrentRestTypeName> ()
    builder.Build () |> this.AddSingleton

  /// <summary>
  /// Registers default REST implementation as services.
  /// <summary>
  /// <param name="this">Service container.</param>
  /// <returns>Service container reference for chaining.</returns>
  [<Extension>]
  static member AddDefaultRestServices (this : IServiceCollection) =
    this.TryAddScoped (typedefof<IDeserializer<_>>, typedefof<DefaultDeserializer<_>>)
    this.TryAddScoped (typedefof<IRestQueryFilter<_>>, typedefof<DefaultQueryFilter<_>>)
    this.TryAddScoped (typedefof<IRestQueryOrderer<_>>, typedefof<DefaultQueryOrderer<_>>)
    this.TryAddSingleton (typedefof<IDefaultOrderProperty>, typedefof<DefaultDefaultOrderProperty>)
    this.TryAddScoped (typedefof<IRestItem<_,_>>, typedefof<DefaultRestItem<_,_>>)
    this.TryAddScoped (typedefof<IRestCreate<_,_>>, typedefof<DefaultRestCreate<_,_>>)
    this.TryAddScoped (typedefof<IRestUpdate<_,_>>, typedefof<DefaultRestUpdate<_,_>>)
    this.TryAddScoped (typedefof<IRestDelete<_,_>>, typedefof<DefaultRestDelete<_,_>>)
    this.TryAddScoped (typedefof<IRestListCollection<_>>, typedefof<DefaultRestListCollection<_>>)
    this
