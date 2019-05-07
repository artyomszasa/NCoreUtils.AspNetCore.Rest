namespace NCoreUtils.AspNetCore.Rest

open System.Diagnostics.CodeAnalysis
open Microsoft.Extensions.Logging
open NCoreUtils
open NCoreUtils.AspNetCore
open NCoreUtils.Logging

/// Holds unresolved type name of the entity being created. Should be registered as scoped service.
[<CLIMutable>]
[<NoEquality; NoComparison>]
type CurrentRestTypeName = {
  /// Gets or sets unresolved type name of the entity being created.
  mutable Value : CaseInsensitive }

[<AutoOpen>]
module internal CurrentRestTypeName =

  [<ExcludeFromCodeCoverage>]
  let inline setCurrentRestTypeName value (inst : CurrentRestTypeName) =
    inst.Value <- value

type internal ManagedTypeBinder (configuration : RestConfiguration, currentTypeName : CurrentRestTypeName, logger : ILogger<ManagedTypeBinder>) =
  interface IValueBinder with
    member __.AsyncBind (_, _) =
      match CaseInsensitive.Empty = currentTypeName.Value with
      | true -> invalidOp "No current rest type name while binding rest type parameter."
      | _    ->
      match Map.tryFind currentTypeName.Value configuration.ManagedTypes with
      | Some ``type`` ->
        debugf logger "Type name %s resolved to type %A." (currentTypeName.Value.ToLowerString()) ``type``
        box ``type`` |> async.Return
      | _ ->
        currentTypeName.Value.ToLowerString ()
        |> sprintf "No managed type registered for %s"
        |> NotFoundException
        |> raise
