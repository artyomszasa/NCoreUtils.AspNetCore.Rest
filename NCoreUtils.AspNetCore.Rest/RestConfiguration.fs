namespace NCoreUtils.AspNetCore.Rest

open System
open System.Collections.Generic
open NCoreUtils
open System.Security.Claims

[<NoEquality; NoComparison>]
type RestAccessConfiguration = {
  Global : IAccessValidator
  Create : IAccessValidator
  Update : IAccessValidator
  Delete : IAccessValidator
  Item   : IAccessValidator
  List   : IAccessValidator }

module RestAccessConfiguration =

  [<CompiledName("Allow")>]
  let allow =
    { new IAccessValidator with
        member __.AsyncValidate (_, _) = async.Return true
    }

  [<CompiledName("Deny")>]
  let deny =
    { new IAccessValidator with
        member __.AsyncValidate (_, _) = async.Return false
    }

  [<CompiledName("AllowAll")>]
  let allowAll =
    { Global = allow
      Create = allow
      Update = allow
      Delete = allow
      Item   = allow
      List   = allow }

type RestAccessConfigurationBuilder (access : RestAccessConfiguration) =
  let mutable access = access
  new () = RestAccessConfigurationBuilder RestAccessConfiguration.allowAll
  member __.Configuration with get () = access and set value = access <- value
  member this.ConfigureGlobal validator = access <- { access with Global = validator }; this
  member this.ConfigureCreate validator = access <- { access with Create = validator }; this
  member this.ConfigureUpdate validator = access <- { access with Update = validator }; this
  member this.ConfigureDelete validator = access <- { access with Delete = validator }; this
  member this.ConfigureItem   validator = access <- { access with Item   = validator }; this
  member this.ConfigureList   validator = access <- { access with List   = validator }; this
  member __.Build () = access

[<NoEquality; NoComparison>]
type RestConfiguration = {
  PathPrefix          : CaseInsensitive list
  ManagedTypes        : Map<CaseInsensitive, Type>
  AccessConfiguration : RestAccessConfiguration }

type RestConfigurationBuilder () =
  static let sepArray = [| '/' |]

  let managedTypes = Dictionary<CaseInsensitive, Type> ()
  let mutable pathPrefix = []

  member val AccessConfiguration = RestAccessConfigurationBuilder ()

  member this.Add (name : CaseInsensitive, ``type`` : Type) =
    managedTypes.Add (name, ``type``)
    this

  member this.Add (``type`` : Type) =
    this.Add (``type``.Name |> CaseInsensitive, ``type``)

  member this.AddRange (types : seq<Type>) =
    for ``type`` in types do
      this.Add ``type`` |> ignore
    this

  member this.AddRange (types : seq<Type>, nameFactory : Func<Type, CaseInsensitive>) =
    for ``type`` in types do
      this.Add (nameFactory.Invoke ``type``, ``type``) |> ignore
    this

  member this.WithPathPrefix (prefix : CaseInsensitive) =
    pathPrefix <-
      prefix.Value.Split (sepArray, StringSplitOptions.RemoveEmptyEntries)
      |> Seq.map CaseInsensitive
      |> Seq.toList
    this

  member this.ConfigureAccess (configure : Action<RestAccessConfigurationBuilder>) =
    configure.Invoke this.AccessConfiguration
    this

  member this.ConfigureAccess (configure : RestAccessConfigurationBuilder -> RestAccessConfigurationBuilder) =
    configure this.AccessConfiguration |> ignore
    this


  member this.Build () =
    { ManagedTypes        = managedTypes |> Seq.fold (fun map kv -> Map.add kv.Key kv.Value map) Map.empty
      PathPrefix          = pathPrefix
      AccessConfiguration = this.AccessConfiguration.Build () }

[<AutoOpen>]
module RestAccessConfigurationBuilderExt =

  let mkAccessValidator (asyncValidate : IServiceProvider -> ClaimsPrincipal -> Async<bool>) =
    { new IAccessValidator with
        member __.AsyncValidate (serviceProvider, principal) = asyncValidate serviceProvider principal
    }

open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Tasks
open System.Reflection

type private FA2 = Func<IServiceProvider, ClaimsPrincipal, CancellationToken, Task<bool>>
type private FA1 = Func<ClaimsPrincipal, CancellationToken, Task<bool>>
type private FS2 = Func<IServiceProvider, ClaimsPrincipal, bool>
type private FS1 = Func<ClaimsPrincipal, bool>

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

  [<Extension>]
  static member ConfigureGlobal (this : RestAccessConfigurationBuilder, validate : FA1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureGlobal
  [<Extension>]
  static member ConfigureGlobal (this : RestAccessConfigurationBuilder, validate : FA2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureGlobal
  [<Extension>]
  static member ConfigureGlobal (this : RestAccessConfigurationBuilder, validate : FS1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureGlobal
  [<Extension>]
  static member ConfigureGlobal (this : RestAccessConfigurationBuilder, validate : FS2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureGlobal


  [<Extension>]
  static member ConfigureCreate (this : RestAccessConfigurationBuilder, validate : FA1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureCreate
  [<Extension>]
  static member ConfigureCreate (this : RestAccessConfigurationBuilder, validate : FA2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureCreate
  [<Extension>]
  static member ConfigureCreate (this : RestAccessConfigurationBuilder, validate : FS1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureCreate
  [<Extension>]
  static member ConfigureCreate (this : RestAccessConfigurationBuilder, validate : FS2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureCreate

  [<Extension>]
  static member ConfigureUpdate (this : RestAccessConfigurationBuilder, validate : FA1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureUpdate
  [<Extension>]
  static member ConfigureUpdate (this : RestAccessConfigurationBuilder, validate : FA2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureUpdate
  [<Extension>]
  static member ConfigureUpdate (this : RestAccessConfigurationBuilder, validate : FS1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureUpdate
  [<Extension>]
  static member ConfigureUpdate (this : RestAccessConfigurationBuilder, validate : FS2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureUpdate

  static member ConfigureDelete (this : RestAccessConfigurationBuilder, validate : FA1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureDelete
  [<Extension>]
  static member ConfigureDelete (this : RestAccessConfigurationBuilder, validate : FA2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureDelete
  [<Extension>]
  static member ConfigureDelete (this : RestAccessConfigurationBuilder, validate : FS1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureDelete
  [<Extension>]
  static member ConfigureDelete (this : RestAccessConfigurationBuilder, validate : FS2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureDelete

  [<Extension>]
  static member ConfigureList (this : RestAccessConfigurationBuilder, validate : FA1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureList
  [<Extension>]
  static member ConfigureList (this : RestAccessConfigurationBuilder, validate : FA2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureList
  [<Extension>]
  static member ConfigureList (this : RestAccessConfigurationBuilder, validate : FS1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureList
  [<Extension>]
  static member ConfigureList (this : RestAccessConfigurationBuilder, validate : FS2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureList

  [<Extension>]
  static member ConfigureItem (this : RestAccessConfigurationBuilder, validate : FA1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureItem
  [<Extension>]
  static member ConfigureItem (this : RestAccessConfigurationBuilder, validate : FA2) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureItem
  [<Extension>]
  static member ConfigureItem (this : RestAccessConfigurationBuilder, validate : FS1) = RestAccessConfigurationBuilderExtensions.OfTask validate |> this.ConfigureItem
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


  [<Extension>]
  [<RequiresExplicitTypeArguments>]
  static member ConfigureGlobal<'validation when 'validation :> IAccessValidation> (this : RestAccessConfigurationBuilder) =
    RestAccessConfigurationBuilderExtensions.CreateAccessValidator<'validation> ()
    |> this.ConfigureGlobal

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
