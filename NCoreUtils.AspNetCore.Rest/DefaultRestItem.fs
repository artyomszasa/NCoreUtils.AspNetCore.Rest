namespace NCoreUtils.AspNetCore.Rest

open System.Runtime.CompilerServices
open NCoreUtils.Data
open NCoreUtils.AspNetCore

/// <summary>
/// Provides default implementation for REST ITEM method.
/// </summary>
/// <typeparam name="a">Type of the target object.</typeparam>
/// <typeparam name="id">Type of the Id property of the target object.</typeparam>
type DefaultRestItem<'a, 'id when 'a :> IHasId<'id> and 'id : equality> =
  val private repository : IDataRepository<'a, 'id>

  /// <summary>
  /// Initializes new instance from the specified parameters.
  /// </summary>
  /// <param name="repository">Repository to use.</param>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  new (repository) = { repository = repository }

  abstract AsyncInvoke : id:'id -> Async<'a>

  /// <summary>
  /// Performes REST ITEM action for the predefined type.
  /// </summary>
  /// <param name="id">Id of the object to return.</param>
  /// <returns>
  /// Object of the specified type for the specified id.
  /// </returns>
  default this.AsyncInvoke id = async {
    let! item = this.repository.AsyncLookup id
    return
      match box item with
      | null -> NotFoundException () |> raise
      | _    -> item }

  interface IRestItem<'a, 'id> with
    member this.AsyncInvoke id = this.AsyncInvoke id
