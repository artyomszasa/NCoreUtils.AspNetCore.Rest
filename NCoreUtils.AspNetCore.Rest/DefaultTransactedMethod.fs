namespace NCoreUtils.AspNetCore.Rest

open NCoreUtils.Data

/// <summary>
/// Provides base class for default implementation of transacted REST methods.
/// </summary>
/// <typeparam name="a">Type of the target object.</typeparam>
/// <typeparam name="id">Type of the Id property of the target object.</typeparam>
[<AbstractClass>]
type DefaultTransactedMethod<'a, 'id when 'a :> IHasId<'id> and 'id : equality> =
  val private repository : IDataRepository<'a, 'id>

  /// <summary>
  /// Initializes new instance from the specified parameters.
  /// </summary>
  /// <param name="repository">Repository to use.</param>
  new (repository) = { repository = repository }

  /// Gets underlying data repository.
  member this.Repository = this.repository

  abstract AsyncBeginTransaction : unit -> Async<IDataTransaction>

  /// <summary>
  /// Initiates transaction through data repository context.
  /// </summary>
  /// <returns>Transaction to use.</returns>
  default this.AsyncBeginTransaction () = this.repository.Context.AsyncBeginTransaction System.Data.IsolationLevel.ReadCommitted
  interface IRestTrasactedMethod with
    member this.AsyncBeginTransaction () = this.AsyncBeginTransaction ()
