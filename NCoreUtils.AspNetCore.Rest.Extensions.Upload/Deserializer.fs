namespace NCoreUtils.AspNetCore.Rest

open System
open System.Collections.Concurrent
open System.IO
open System.Reflection
open Microsoft.Extensions.DependencyInjection
open NCoreUtils

[<AutoOpen>]
module private UploadDeserializerHelpers =

  let private cache = ConcurrentDictionary ()

  let getPublicInstanceProperties (ty : Type) =
    let mutable properties = Unchecked.defaultof<_>
    if not (cache.TryGetValue (ty, &properties)) then
      properties <- ty.GetProperties (BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.FlattenHierarchy)
      cache.TryAdd (ty, properties) |> ignore
    properties

  let inline ``for``<'a> (deserializer : IDeserializer) = deserializer.For<'a> ()

  [<RequiresExplicitTypeArguments>]
  let inline diActivate<'a> serviceProvider = ActivatorUtilities.CreateInstance<'a> serviceProvider


type DataUploadDeserializer<'TData, 'TUploadData when 'TUploadData :> IHasUploadData and 'TData : (new : unit -> 'TData)> =
  val private serviceProvider : IServiceProvider
  val private currentUploadData : CurrentUploadData<'TData>

  new (serviceProvider, currentUploadData) =
    { serviceProvider   = serviceProvider
      currentUploadData = currentUploadData }

  member this.Deserialize (stream : Stream) =
    let deserializer =
      tryGetService<IDeserializer<'TUploadData>> this.serviceProvider
      |> Option.orElseWith  (fun () -> tryGetService<IDeserializer> this.serviceProvider >>| ``for``<'TUploadData>)
      |> Option.defaultWith (fun () -> diActivate<DefaultDeserializer<'TUploadData>> this.serviceProvider :> _)
    let uploadData = deserializer.Deserialize stream
    this.currentUploadData.Value <- uploadData.Data
    let result = new 'TData ()
    let properties = getPublicInstanceProperties typeof<'TData>
    for property in properties do
      property.SetValue (result, property.GetValue (uploadData, null), null)
    result
  interface IDeserializer<'TData> with
    member this.Deserialize stream = this.Deserialize stream




