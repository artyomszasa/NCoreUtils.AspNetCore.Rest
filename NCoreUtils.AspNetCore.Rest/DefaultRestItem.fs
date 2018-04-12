namespace NCoreUtils.AspNetCore.Rest

open NCoreUtils.Data
open NCoreUtils.AspNetCore

type DefaultRestItem<'a, 'id when 'a :> IHasId<'id> and 'id : equality> (repository : IDataRepository<'a, 'id>) =

  interface IRestItem<'a, 'id> with
    member __.AsyncInvoke id = async {
      let! item = repository.AsyncLookup id
      return
        match box item with
        | null -> NotFoundException () |> raise
        | _    -> item }
