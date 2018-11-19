namespace NCoreUtils.AspNetCore.Rest

open System.Runtime.CompilerServices
open Microsoft.Extensions.Logging
open NCoreUtils.AspNetCore
open NCoreUtils.Data
open NCoreUtils.Logging

/// <summary>
/// Provides default implementation for REST DELETE method.
/// </summary>
/// <typeparam name="a">Type of the target object.</typeparam>
/// <typeparam name="id">Type of the Id property of the target object.</typeparam>
type DefaultRestDelete<'a, 'id when 'a :> IHasId<'id> and 'id : equality> =
  inherit DefaultTransactedMethod<'a, 'id>
  val private logger : ILogger

  /// <summary>
  /// Initializes new instance from the specified parameters.
  /// </summary>
  /// <param name="repository">Repository to use.</param>
  /// <param name="loggerFactory">Logger factory.</param>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  new (repository, loggerFactory : ILoggerFactory) =
    { inherit DefaultTransactedMethod<'a, 'id> (repository)
      logger = loggerFactory.CreateLogger "NCoreUtils.AspNetCore.Rest.DefaultRestDelete" }

  abstract AsyncInvoke : id:'id -> Async<unit>

  /// <summary>
  /// Performes REST DELETE action for the predefined type.
  /// </summary>
  /// <param name="id">Id of the object to delete.</param>
  default this.AsyncInvoke id = async {
    let! item = this.Repository.AsyncLookup id
    match box item with
    | null ->
      debugf this.logger "No entity of type %s found for key = %A (data-delete)." typeof<'a>.FullName id
      NotFoundException () |> raise
    | _ ->
      do! this.Repository.AsyncRemove item
      debugf this.logger "Successfully removed entity of type %s with key = %A (data-delete)." typeof<'a>.FullName id }

  interface IRestDelete<'a, 'id> with
    member this.AsyncInvoke id = this.AsyncInvoke id
  interface IBoxedInvoke<'id, unit> with
    member this.Instance = box this
    member this.AsyncInvoke id = this.AsyncInvoke id