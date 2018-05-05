namespace NCoreUtils.AspNetCore.Rest

open System
open NCoreUtils

[<Sealed>]
type CurrentUploadData<'T> () =
  member val Value = null : byte[] with get, set
  member this.IsEmpty = isNull this.Value

[<Interface>]
type IHasUploadData =
  abstract Data : byte[] with get, set

type UploadTypeMap = UploadTypeMap of (struct (Type * Type)) list

module UploadTypeMap =

  let inline private fstEq ty (struct (a, _)) = ty = a

  let inline private ssnd (struct (_, x)) = x

  let tryFind sourceType (UploadTypeMap list) =
    List.tryFind (fstEq sourceType) list
    >>| ssnd