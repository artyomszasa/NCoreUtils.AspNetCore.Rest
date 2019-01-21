namespace NCoreUtils.AspNetCore.Rest

open System.Runtime.CompilerServices
open Microsoft.Extensions.Logging
open NCoreUtils
open NCoreUtils.AspNetCore
open NCoreUtils.Data
open NCoreUtils.Logging

type VariableAttribute () = inherit System.Attribute ()

/// <summary>
/// Provides default implementation for REST UPDATE method.
/// </summary>
/// <typeparam name="a">Type of the target object.</typeparam>
/// <typeparam name="id">Type of the Id property of the target object.</typeparam>
type DefaultRestUpdate<'a, 'id when 'a :> IHasId<'id> and 'id : equality> =
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
      logger = loggerFactory.CreateLogger "NCoreUtils.AspNetCore.Rest.DefaultRestUpdate" }

  abstract AsyncInvoke : id:'id * data:'a -> Async<'a>

  /// <summary>
  /// Performes REST UPDATE action for the specified type.
  /// </summary>
  /// <typeparam name="a">Type of the target object.</typeparam>
  /// <typeparam name="id">Type of the Id property of the target object.</typeparam>
  /// <param name="id">Id of the object to update.</param>
  /// <param name="data">Object to update in dataset.</param>
  /// <returns>
  /// Object returned by dataset after update operation. Depending on the repository implementation some of the values
  /// of the returned object may differ from the input.
  /// </returns>
  default this.AsyncInvoke (id, data) = async {
    // check that data has the same id
    do if id <> data.Id then BadRequestException "Entity data has invalid id." |> raise
    // check whether entity with specified id exists
    let! exists = this.Repository.AsyncLookup data.Id >>| (box >> isNull >> not)
    // if not --> raise error
    do if not exists then NotFoundException () |> raise
    // if exists --> persists new value
    let! item = this.Repository.AsyncPersist data
    // log success
    debugf this.logger "Entity of type %s with key = %A has been updated (data-update)." typeof<'a>.FullName item.Id
    // return result
    return item }


  interface IRestUpdate<'a, 'id> with
    member this.AsyncInvoke (id, data) = this.AsyncInvoke (id, data)
  interface IBoxedInvoke<'id, 'a, 'a> with
    member this.Instance = box this
    member this.AsyncInvoke (id, data) = this.AsyncInvoke (id, data)
