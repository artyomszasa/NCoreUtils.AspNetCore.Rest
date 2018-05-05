namespace NCoreUtils.AspNetCore.Rest

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection

[<Extension>]
[<Sealed; AbstractClass>]
type ServiceCollectionRestUploadExtensions =

  [<Extension>]
  static member AddRestDataUploadDeserializer<'TData when 'TData : (new : unit -> 'TData)>(services : IServiceCollection) =
    let dataUploadType = Emitter.emitDataUploadType typeof<'TData>
    let deserializerType = typedefof<DataUploadDeserializer<_, _>>.MakeGenericType [| typeof<'TData>; dataUploadType |]
    services
      .AddScoped<CurrentUploadData<'TData>>()
      .AddScoped(typeof<IDeserializer<'TData>>, deserializerType)