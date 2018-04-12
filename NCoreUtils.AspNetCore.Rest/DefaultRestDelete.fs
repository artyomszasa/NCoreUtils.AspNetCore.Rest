namespace NCoreUtils.AspNetCore.Rest

open Microsoft.Extensions.Logging
open NCoreUtils.AspNetCore
open NCoreUtils.Data
open NCoreUtils.Logging

type DefaultRestDelete<'a, 'id when 'a :> IHasId<'id> and 'id : equality> (repository : IDataRepository<'a, 'id>, loggerFactory : ILoggerFactory) =
  let logger = loggerFactory.CreateLogger "NCoreUtils.AspNetCore.Rest.DefaultRestDelete"
  interface IRestDelete<'a, 'id> with
    member __.AsyncBeginTransaction () = repository.Context.AsyncBeginTransaction System.Data.IsolationLevel.ReadCommitted
    member __.AsyncInvoke id = async {
      let! item = repository.AsyncLookup id
      match box item with
      | null ->
        debugf logger "No entity of type %s found for key = %A (data-delete)." typeof<'a>.FullName id
        NotFoundException () |> raise
      | _ ->
        do! repository.AsyncRemove item
        debugf logger "Successfully removed entity of type %s with key = %A (data-delete)." typeof<'a>.FullName id }