namespace NCoreUtils.AspNetCore.Rest

open System.Runtime.CompilerServices
open Microsoft.Extensions.Logging
open NCoreUtils.AspNetCore
open NCoreUtils.Data
open NCoreUtils.Linq
open NCoreUtils.Logging

/// <summary>
/// Provides default implementation for REST CREATE method.
/// </summary>
/// <typeparam name="a">Type of the target object.</typeparam>
/// <typeparam name="id">Type of the Id property of the target object.</typeparam>
type DefaultRestCreate<'a, 'id when 'a :> IHasId<'id> and 'id : equality> =
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
      logger     = loggerFactory.CreateLogger "NCoreUtils.AspNetCore.Rest.DefaultRestCreate" }

  abstract AsyncInvoke : data:'a -> Async<'a>

  /// <summary>
  /// Performes REST CREATE action for the predefined type.
  /// </summary>
  /// <param name="data">Object to insert into dataset.</param>
  /// <returns>
  /// Object returned by dataset after insert operation. Depending on the repository implementation some of the values
  /// of the returned object may differ from the input.
  /// </returns>
  default this.AsyncInvoke (data : 'a) = async {
    // check if already exists
    if data.HasValidId () then
      let! exists =
        let id = data.Id
        this.Repository.Items |> Q.asyncExists (fun item -> item.Id = id)
      do
        match exists with
        | true ->
          debugf this.logger "Entity of type %s with key = %A already exists (data-create)." typeof<'a>.FullName data.Id
          ConflictException "Entity already exists." |> raise
        | _ -> () // BadRequestException "New entities must not include id." |> raise
    // persist entity
    let! item = this.Repository.AsyncPersist data
    debugf this.logger "Entity of type %s has been created with key = %A (data-create)." typeof<'a>.FullName item.Id
    return item }
  interface IRestCreate<'a, 'id> with
    member this.AsyncInvoke data = this.AsyncInvoke data
  interface IBoxedInvoke<'a, 'a> with
    member this.Instance = box this
    member this.AsyncInvoke data = this.AsyncInvoke data
