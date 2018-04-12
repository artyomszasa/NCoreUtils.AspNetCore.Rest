namespace NCoreUtils.AspNetCore.Rest

open System
open System.Collections.Generic
open NCoreUtils

type RestConfiguration = {
  PathPrefix   : CaseInsensitive list
  ManagedTypes : Map<CaseInsensitive, Type> }

type RestConfigurationBuilder () =
  static let sepArray = [| '/' |]

  let managedTypes = Dictionary<CaseInsensitive, Type> ()
  let mutable pathPrefix = []
  member this.Add (name : CaseInsensitive, ``type`` : Type) =
    managedTypes.Add (name, ``type``)
    this

  member this.Add (``type`` : Type) =
    this.Add (``type``.Name |> CaseInsensitive, ``type``)

  member this.AddRange (types : seq<Type>) =
    for ``type`` in types do
      this.Add ``type`` |> ignore
    this

  member this.AddRange (types : seq<Type>, nameFactory : Func<Type, CaseInsensitive>) =
    for ``type`` in types do
      this.Add (nameFactory.Invoke ``type``, ``type``) |> ignore
    this

  member this.WithPathPrefix (prefix : CaseInsensitive) =
    pathPrefix <-
      prefix.Value.Split (sepArray, StringSplitOptions.RemoveEmptyEntries)
      |> Seq.map CaseInsensitive
      |> Seq.toList
    this

  member __.Build () =
    { ManagedTypes = managedTypes |> Seq.fold (fun map kv -> Map.add kv.Key kv.Value map) Map.empty
      PathPrefix   = pathPrefix }

