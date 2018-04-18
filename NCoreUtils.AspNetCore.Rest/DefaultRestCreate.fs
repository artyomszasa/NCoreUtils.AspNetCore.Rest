namespace NCoreUtils.AspNetCore.Rest

open Microsoft.Extensions.Logging
open NCoreUtils.AspNetCore
open NCoreUtils.Data
open NCoreUtils.Linq
open NCoreUtils.Logging

type DefaultRestCreate<'a, 'id when 'a :> IHasId<'id> and 'id : equality> (repository : IDataRepository<'a, 'id>, loggerFactory : ILoggerFactory) =
  let logger = loggerFactory.CreateLogger "NCoreUtils.AspNetCore.Rest.DefaultRestCreate"
  interface IRestCreate<'a, 'id> with
    member __.AsyncBeginTransaction () = repository.Context.AsyncBeginTransaction System.Data.IsolationLevel.ReadCommitted
    member __.AsyncInvoke data = async {
      // check if already exists
      if data.HasValidId () then
        let! exists = repository.Items |> Q.asyncExists <@ fun item -> item.Id = data.Id @>
        do
          match exists with
          | true ->
            debugf logger "Entity of type %s with key = %A already exists (data-create)." typeof<'a>.FullName data.Id
            ConflictException "Entity already exists." |> raise
          | _ -> () // BadRequestException "New entities must not include id." |> raise
      // persist entity
      let! item = repository.AsyncPersist data
      debugf logger "Entity of type %s has been created with key = %A (data-create)." typeof<'a>.FullName item.Id
      return item }
