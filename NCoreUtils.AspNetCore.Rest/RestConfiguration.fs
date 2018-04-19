namespace NCoreUtils.AspNetCore.Rest

open System
open System.Collections.Generic
open NCoreUtils
open System.Security.Claims
open System.Runtime.CompilerServices

/// Represents REST access validation configuration.
[<NoEquality; NoComparison>]
type RestAccessConfiguration = {
  /// Gets global access validator.
  Global : IAccessValidator
  /// Gets access validator for REST CREATE method.
  Create : IAccessValidator
  /// Gets access validator for REST UPDATE method.
  Update : IAccessValidator
  /// Gets access validator for REST DELETE method.
  Delete : IAccessValidator
  /// Gets access validator for REST ITEM method.
  Item   : IAccessValidator
  /// Gets access validator for REST LIST method.
  List   : IAccessValidator }

/// Contains predefined access validation values.
[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module RestAccessConfiguration =

  /// Gets access validator that unconditionally allows access.
  [<CompiledName("Allow")>]
  let allow =
    { new IAccessValidator with
        member __.AsyncValidate (_, _) = async.Return true
    }

  /// Gets access validator that unconditionally denies acess.
  [<CompiledName("Deny")>]
  let deny =
    { new IAccessValidator with
        member __.AsyncValidate (_, _) = async.Return false
    }

  /// Gets access validator that unconditionally allows access to all operations.
  [<CompiledName("AllowAll")>]
  let allowAll =
    { Global = allow
      Create = allow
      Update = allow
      Delete = allow
      Item   = allow
      List   = allow }

/// Provides mutable object capable of configuring REST access configuration.
type RestAccessConfigurationBuilder  =
  val mutable private access : RestAccessConfiguration
  /// <summary>
  /// Initializes new instance from specified access configuration.
  /// <summary>
  /// <param name="access">Initial access configuration.</param>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  new (access) = { access = access }
  /// <summary>
  /// Initializes new instance with allow all access configuration.
  /// <summary>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  new () = RestAccessConfigurationBuilder RestAccessConfiguration.allowAll
  /// Gets or sets underlying immutable REST access configuration.
  member this.Configuration with get () = this.access and set value = this.access <- value
  /// <summary>
  /// Replaces global access validator with the spcified one.
  /// <summary>
  /// <param name="validator">Access validator.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.ConfigureGlobal validator = this.access <- { this.access with Global = validator }; this
  /// <summary>
  /// Replaces REST CREATE method access validator with the spcified one.
  /// <summary>
  /// <param name="validator">Access validator.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.ConfigureCreate validator = this.access <- { this.access with Create = validator }; this
  /// <summary>
  /// Replaces REST UPDATE method access validator with the spcified one.
  /// <summary>
  /// <param name="validator">Access validator.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.ConfigureUpdate validator = this.access <- { this.access with Update = validator }; this
  /// <summary>
  /// Replaces REST DELETE method access validator with the spcified one.
  /// <summary>
  /// <param name="validator">Access validator.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.ConfigureDelete validator = this.access <- { this.access with Delete = validator }; this
  /// <summary>
  /// Replaces REST ITEM method access validator with the spcified one.
  /// <summary>
  /// <param name="validator">Access validator.</param>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.ConfigureItem   validator = this.access <- { this.access with Item   = validator }; this
  /// <summary>
  /// Replaces REST LIST method access validator with the spcified one.
  /// <summary>
  /// <param name="validator">Access validator.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.ConfigureList   validator = this.access <- { this.access with List   = validator }; this
  /// Gets the underlying immutable REST access configuration.
  member this.Build () = this.access

/// Represents REST pipeline configuration
[<NoEquality; NoComparison>]
type RestConfiguration = {
  /// Path prefix that defines which request uris should be processed.
  PathPrefix          : CaseInsensitive list
  /// Contains typnames that should be recognized az entity types by the REST pipeline.
  ManagedTypes        : Map<CaseInsensitive, Type>
  /// Provides REST access configuration.
  AccessConfiguration : RestAccessConfiguration }

/// Provides mutable object capable of configuring REST pipeline configuration.
type RestConfigurationBuilder () =
  static let sepArray = [| '/' |]

  let managedTypes = Dictionary<CaseInsensitive, Type> ()
  let mutable pathPrefix = []

  /// Gets REST access configuration builder.
  member val AccessConfiguration = RestAccessConfigurationBuilder ()

  /// <summary>
  /// Adds handled entity type with the specified parameters.
  /// </summary>
  /// <param name="name">Name that sould be recognized as type specified by <paramref name="type" /> parameter.</param>
  /// <param name="type">Entity type.</param>
  /// <returns>Builder reference for chaining.</returns>
  member this.Add (name : CaseInsensitive, ``type`` : Type) =
    managedTypes.Add (name, ``type``)
    this

  /// <summary>
  /// Adds handled entity type. Name of entity type with be used for type name recognition.
  /// </summary>
  /// <param name="type">Entity type.</param>
  /// <returns>Builder reference for chaining.</returns>
  member this.Add (``type`` : Type) =
    this.Add (``type``.Name |> CaseInsensitive, ``type``)

  /// <summary>
  /// Adds handled entity types. Name of entity type with be used for type name recognition.
  /// </summary>
  /// <param name="types">Entity types.</param>
  /// <returns>Builder reference for chaining.</returns>
  member this.AddRange (types : seq<Type>) =
    for ``type`` in types do
      this.Add ``type`` |> ignore
    this

  /// <summary>
  /// Adds handled entity types. <paramref name="nameFactory"> is used to retrive entity type names that will be used
  /// for type name recognition.
  /// </summary>
  /// <param name="types">Entity types.</param>
  /// <param name="nameFactory">Name factory that provides type names for type recognition.</param>
  /// <returns>Builder reference for chaining.</returns>
  member this.AddRange (types : seq<Type>, nameFactory : Func<Type, CaseInsensitive>) =
    for ``type`` in types do
      this.Add (nameFactory.Invoke ``type``, ``type``) |> ignore
    this

  /// <summary>
  /// Replaces path prefix with the on specified.
  /// </summary>
  /// <param name="prefix">Path prefix.</param>
  /// <returns>Builder reference for chaining.</returns>
  member this.WithPathPrefix (prefix : CaseInsensitive) =
    pathPrefix <-
      prefix.Value.Split (sepArray, StringSplitOptions.RemoveEmptyEntries)
      |> Seq.map CaseInsensitive
      |> Seq.toList
    this

  /// <summary>
  /// Configures access configuration using the specified configuration function.
  /// </summary>
  /// <param name="configure">Function that configures access configuration.</param>
  /// <returns>Builder reference for chaining.</returns>
  member this.ConfigureAccess (configure : Action<RestAccessConfigurationBuilder>) =
    configure.Invoke this.AccessConfiguration
    this

  /// <summary>
  /// Configures access configuration using the specified configuration function.
  /// </summary>
  /// <param name="configure">Function that configures access configuration.</param>
  /// <returns>Builder reference for chaining.</returns>
  member this.ConfigureAccess (configure : RestAccessConfigurationBuilder -> RestAccessConfigurationBuilder) =
    configure this.AccessConfiguration |> ignore
    this


  /// <summary>
  /// Builds immutable REST configuration object.
  /// </summary>
  /// <returns>REST configuration object.</returns>
  member this.Build () =
    { ManagedTypes        = managedTypes |> Seq.fold (fun map kv -> Map.add kv.Key kv.Value map) Map.empty
      PathPrefix          = pathPrefix
      AccessConfiguration = this.AccessConfiguration.Build () }

/// Provides F# firendly extensions for REST access configuration.
[<AutoOpen>]
module RestAccessConfigurationBuilderExt =

  let mkAccessValidator (asyncValidate : IServiceProvider -> ClaimsPrincipal -> Async<bool>) =
    { new IAccessValidator with
        member __.AsyncValidate (serviceProvider, principal) = asyncValidate serviceProvider principal
    }

open System.Threading
open System.Threading.Tasks
open System.Reflection

type private FA2 = Func<IServiceProvider, ClaimsPrincipal, CancellationToken, Task<bool>>
type private FA1 = Func<ClaimsPrincipal, CancellationToken, Task<bool>>
type private FS2 = Func<IServiceProvider, ClaimsPrincipal, bool>
type private FS1 = Func<ClaimsPrincipal, bool>

/// Provides extensions for REST access configuration.
[<Extension>]
[<Sealed; AbstractClass>]
type RestAccessConfigurationBuilderExtensions =

  static member private OfTask (func : Func<IServiceProvider, ClaimsPrincipal, _, Task<bool>>) =
    { new IAccessValidator with
        member __.AsyncValidate (serviceProvider, principal) =
          Async.Adapt (fun cancellationToken -> func.Invoke (serviceProvider, principal, cancellationToken))
    }
  static member private OfTask (func : Func<IServiceProvider, ClaimsPrincipal, bool>) =
    { new IAccessValidator with
        member __.AsyncValidate (serviceProvider, principal) =
          func.Invoke (serviceProvider, principal) |> async.Return
    }

  static member private OfTask (func : Func<ClaimsPrincipal, _, Task<bool>>) =
    { new IAccessValidator with
        member __.AsyncValidate (_, principal) =
          Async.Adapt (fun cancellationToken -> func.Invoke (principal, cancellationToken))
    }
  static member private OfTask (func : Func<ClaimsPrincipal, bool>) =
    { new IAccessValidator with
        member __.AsyncValidate (_, principal) =
          func.Invoke principal |> async.Return
    }

  /// <summary>
  /// Replaces global access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureGlobal (this : RestAccessConfigurationBuilder, validate : FA1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureGlobal
  /// <summary>
  /// Replaces global access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureGlobal (this : RestAccessConfigurationBuilder, validate : FA2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureGlobal
  /// <summary>
  /// Replaces global access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureGlobal (this : RestAccessConfigurationBuilder, validate : FS1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureGlobal
  /// <summary>
  /// Replaces global access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureGlobal (this : RestAccessConfigurationBuilder, validate : FS2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureGlobal


  /// <summary>
  /// Replaces REST CREATE method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureCreate (this : RestAccessConfigurationBuilder, validate : FA1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureCreate
  /// <summary>
  /// Replaces REST CREATE method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureCreate (this : RestAccessConfigurationBuilder, validate : FA2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureCreate
  /// <summary>
  /// Replaces REST CREATE method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureCreate (this : RestAccessConfigurationBuilder, validate : FS1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureCreate
  /// <summary>
  /// Replaces REST CREATE method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureCreate (this : RestAccessConfigurationBuilder, validate : FS2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureCreate

  /// <summary>
  /// Replaces REST UPDATE method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureUpdate (this : RestAccessConfigurationBuilder, validate : FA1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureUpdate
  /// <summary>
  /// Replaces REST UPDATE method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureUpdate (this : RestAccessConfigurationBuilder, validate : FA2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureUpdate
  /// <summary>
  /// Replaces REST UPDATE method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureUpdate (this : RestAccessConfigurationBuilder, validate : FS1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureUpdate
  /// <summary>
  /// Replaces REST UPDATE method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureUpdate (this : RestAccessConfigurationBuilder, validate : FS2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureUpdate

  /// <summary>
  /// Replaces REST DELETE method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureDelete (this : RestAccessConfigurationBuilder, validate : FA1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureDelete
  /// <summary>
  /// Replaces REST DELETE method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureDelete (this : RestAccessConfigurationBuilder, validate : FA2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureDelete
  /// <summary>
  /// Replaces REST DELETE method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureDelete (this : RestAccessConfigurationBuilder, validate : FS1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureDelete
  /// <summary>
  /// Replaces REST DELETE method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureDelete (this : RestAccessConfigurationBuilder, validate : FS2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureDelete

  /// <summary>
  /// Replaces REST LIST method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureList (this : RestAccessConfigurationBuilder, validate : FA1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureList
  /// <summary>
  /// Replaces REST LIST method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureList (this : RestAccessConfigurationBuilder, validate : FA2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureList
  /// <summary>
  /// Replaces REST LIST method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureList (this : RestAccessConfigurationBuilder, validate : FS1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureList
  /// <summary>
  /// Replaces REST LIST method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureList (this : RestAccessConfigurationBuilder, validate : FS2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureList

  /// <summary>
  /// Replaces REST ITEM method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureItem (this : RestAccessConfigurationBuilder, validate : FA1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureItem
  /// <summary>
  /// Replaces REST ITEM method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureItem (this : RestAccessConfigurationBuilder, validate : FA2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureItem
  /// <summary>
  /// Replaces REST ITEM method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureItem (this : RestAccessConfigurationBuilder, validate : FS1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureItem
  /// <summary>
  /// Replaces REST ITEM method access validator with the one defined by the specified function.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <param name="validate">Access validator function.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  static member ConfigureItem (this : RestAccessConfigurationBuilder, validate : FS2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureItem

  [<RequiresExplicitTypeArguments>]
  static member private CreateAccessValidator<'validation when 'validation :> IAccessValidation> () =
    { new IAccessValidator with
        member __.AsyncValidate (serviceProvider, principal) = async {
          let struct (validation, shouldDispose) =
            match tryGetService<'validation> serviceProvider with
            | Some validation -> struct (validation, false)
            | _               -> struct (diActivate<'validation> serviceProvider, true)
          try return! validation.AsyncValidate principal
          finally
            match struct (shouldDispose, box validation) with
            | struct (true, (:? IDisposable as disposable)) -> disposable.Dispose ()
            | _ -> () }
    }

  [<RequiresExplicitTypeArguments>]
  static member private CreateEntityAccessValidator<'validation when 'validation :> IEntityAccessValidation> () =
    { new IEntityAccessValidator with
        member __.AsyncValidate (serviceProvider, principal) = async {
          let struct (validation, shouldDispose) =
            match tryGetService<'validation> serviceProvider with
            | Some validation -> struct (validation, false)
            | _               -> struct (diActivate<'validation> serviceProvider, true)
          try return! validation.AsyncValidate principal
          finally
            match struct (shouldDispose, box validation) with
            | struct (true, (:? IDisposable as disposable)) -> disposable.Dispose ()
            | _ -> () }
        member __.AsyncValidate (entity, serviceProvider, principal) = async {
          let struct (validation, shouldDispose) =
            match tryGetService<'validation> serviceProvider with
            | Some validation -> struct (validation, false)
            | _               -> struct (diActivate<'validation> serviceProvider, true)
          try return! validation.AsyncValidate (entity, principal)
          finally
            match struct (shouldDispose, box validation) with
            | struct (true, (:? IDisposable as disposable)) -> disposable.Dispose ()
            | _ -> () }
    }

  [<RequiresExplicitTypeArguments>]
  static member private CreateQueryAccessValidator<'validation when 'validation :> IQueryAccessValidation> () =
    { new IQueryAccessValidator with
        member __.AsyncValidate (serviceProvider, principal) = async {
          let struct (validation, shouldDispose) =
            match tryGetService<'validation> serviceProvider with
            | Some validation -> struct (validation, false)
            | _               -> struct (diActivate<'validation> serviceProvider, true)
          try return! validation.AsyncValidate principal
          finally
            match struct (shouldDispose, box validation) with
            | struct (true, (:? IDisposable as disposable)) -> disposable.Dispose ()
            | _ -> () }
        member __.AsyncFilterQuery (queryable, serviceProvider, principal) = async {
          let struct (validation, shouldDispose) =
            match tryGetService<'validation> serviceProvider with
            | Some validation -> struct (validation, false)
            | _               -> struct (diActivate<'validation> serviceProvider, true)
          try return! validation.AsyncFilterQuery (queryable, principal)
          finally
            match struct (shouldDispose, box validation) with
            | struct (true, (:? IDisposable as disposable)) -> disposable.Dispose ()
            | _ -> () }
    }


  /// <summary>
  /// Replaces global access validator with the one defined by the specified validation.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  [<RequiresExplicitTypeArguments>]
  static member ConfigureGlobal<'validation when 'validation :> IAccessValidation> (this : RestAccessConfigurationBuilder) =
    RestAccessConfigurationBuilderExtensions.CreateAccessValidator<'validation> ()
    |> this.ConfigureGlobal

  /// <summary>
  /// Replaces REST CREATE method access validator with the one defined by the specified validation.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  [<RequiresExplicitTypeArguments>]
  static member ConfigureCreate<'validation when 'validation :> IAccessValidation> (this : RestAccessConfigurationBuilder) =
    let validator =
      match typeof<IEntityAccessValidation>.IsAssignableFrom typeof<'validation> with
      | true ->
        let g = typeof<RestAccessConfigurationBuilderExtensions>.GetMethod ("CreateEntityAccessValidator", BindingFlags.Public ||| BindingFlags.Static)
        let m = g.MakeGenericMethod [| typeof<'validation> |]
        m.Invoke (null, [| |]) :?> IAccessValidator
      | _    -> RestAccessConfigurationBuilderExtensions.CreateAccessValidator<'validation> ()
    this.ConfigureCreate validator

  /// <summary>
  /// Replaces REST UPDATE method access validator with the one defined by the specified validation.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  [<RequiresExplicitTypeArguments>]
  static member ConfigureUpdate<'validation when 'validation :> IAccessValidation> (this : RestAccessConfigurationBuilder) =
    let validator =
      match typeof<IEntityAccessValidation>.IsAssignableFrom typeof<'validation> with
      | true ->
        let g = typeof<RestAccessConfigurationBuilderExtensions>.GetMethod ("CreateEntityAccessValidator", BindingFlags.Public ||| BindingFlags.Static)
        let m = g.MakeGenericMethod [| typeof<'validation> |]
        m.Invoke (null, [| |]) :?> IAccessValidator
      | _    -> RestAccessConfigurationBuilderExtensions.CreateAccessValidator<'validation> ()
    this.ConfigureUpdate validator

  /// <summary>
  /// Replaces REST DELETE method access validator with the one defined by the specified validation.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  [<RequiresExplicitTypeArguments>]
  static member ConfigureDelete<'validation when 'validation :> IAccessValidation> (this : RestAccessConfigurationBuilder) =
    let validator =
      match typeof<IEntityAccessValidation>.IsAssignableFrom typeof<'validation> with
      | true ->
        let g = typeof<RestAccessConfigurationBuilderExtensions>.GetMethod ("CreateEntityAccessValidator", BindingFlags.Public ||| BindingFlags.Static)
        let m = g.MakeGenericMethod [| typeof<'validation> |]
        m.Invoke (null, [| |]) :?> IAccessValidator
      | _    -> RestAccessConfigurationBuilderExtensions.CreateAccessValidator<'validation> ()
    this.ConfigureDelete validator

  /// <summary>
  /// Replaces REST ITEM method access validator with the one defined by the specified validation.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  [<RequiresExplicitTypeArguments>]
  static member ConfigureItem<'validation when 'validation :> IAccessValidation> (this : RestAccessConfigurationBuilder) =
    let validator =
      match typeof<IEntityAccessValidation>.IsAssignableFrom typeof<'validation> with
      | true ->
        let g = typeof<RestAccessConfigurationBuilderExtensions>.GetMethod ("CreateEntityAccessValidator", BindingFlags.Public ||| BindingFlags.Static)
        let m = g.MakeGenericMethod [| typeof<'validation> |]
        m.Invoke (null, [| |]) :?> IAccessValidator
      | _    -> RestAccessConfigurationBuilderExtensions.CreateAccessValidator<'validation> ()
    this.ConfigureItem validator

  /// <summary>
  /// Replaces REST LIST method access validator with the one defined by the specified validation.
  /// <summary>
  /// <param name="this">Access configuration builder.</param>
  /// <returns>Builder reference for chaining.</returns>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<Extension>]
  [<RequiresExplicitTypeArguments>]
  static member ConfigureList<'validation when 'validation :> IAccessValidation> (this : RestAccessConfigurationBuilder) =
    let validator =
      match typeof<IQueryAccessValidation>.IsAssignableFrom typeof<'validation> with
      | true ->
        let g = typeof<RestAccessConfigurationBuilderExtensions>.GetMethod ("CreateQueryAccessValidator", BindingFlags.Public ||| BindingFlags.Static)
        let m = g.MakeGenericMethod [| typeof<'validation> |]
        m.Invoke (null, [| |]) :?> IAccessValidator
      | _    -> RestAccessConfigurationBuilderExtensions.CreateAccessValidator<'validation> ()
    this.ConfigureList validator
