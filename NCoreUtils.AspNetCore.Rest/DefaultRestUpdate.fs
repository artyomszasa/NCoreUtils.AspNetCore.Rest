namespace NCoreUtils.AspNetCore.Rest

open Microsoft.Extensions.Logging
open NCoreUtils.AspNetCore
open NCoreUtils.Data
open NCoreUtils.Linq
open NCoreUtils.Logging


type DefaultRestUpdate<'a, 'id when 'a :> IHasId<'id> and 'id : equality> (repository : IDataRepository<'a, 'id>, loggerFactory : ILoggerFactory) =
  let logger = loggerFactory.CreateLogger "NCoreUtils.AspNetCore.Rest.DefaultRestUpdate"
  interface IRestUpdate<'a, 'id> with
    member __.AsyncBeginTransaction () = repository.Context.AsyncBeginTransaction System.Data.IsolationLevel.ReadCommitted
    member __.AsyncInvoke (id, data) = async {
      // check that data has the same id
      do if id <> data.Id then BadRequestException "Entity data has invalid id." |> raise
      // check whether entity with specified id exists
      let! exists = repository.Items |> Q.asyncExists <@ fun item -> item.Id = id @>
      // if not --> raise error
      do if not exists then NotFoundException () |> raise
      // if exists --> persists new value
      let! item = repository.AsyncPersist data
      // log success
      debugf logger "Entity of type %s with key = %A has been updated (data-update)." typeof<'a>.FullName item.Id
      // return result
      return item }
