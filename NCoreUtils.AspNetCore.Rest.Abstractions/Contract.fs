namespace NCoreUtils.AspNetCore.Rest

open System.Linq
open System.IO
open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks
open NCoreUtils
open NCoreUtils.Data
open System
open System.Collections.Generic

/// Represents property based ordering description.
[<Struct>]
[<StructuralEquality; NoComparison>]
type OrderByProperty = {
  /// Gets property info to order results by.
  Property     : PropertyInfo
  /// Gets whether ordering direction is descending.
  IsDescending : bool }

[<AbstractClass>]
type RestMethodInvocation =
  new () = { }
  abstract ItemType   : Type
  abstract Instance   : obj
  abstract Arguments  : IReadOnlyList<obj>
  abstract ReturnType : Type

[<AbstractClass>]
type RestMethodInvocation<'TResult> =
  inherit RestMethodInvocation
  new () = { inherit RestMethodInvocation () }
  default __.ReturnType = typeof<Async<'TResult>>
  abstract AsyncInvoke : unit -> Async<'TResult>
  abstract UpdateArguments : IReadOnlyList<obj> -> RestMethodInvocation<'TResult>

[<Interface>]
type IRestMethodInvoker =
  abstract AsyncInvoke<'TResult> : target:RestMethodInvocation<'TResult> -> Async<'TResult>

/// Defines functionality for filtering generic queryable based on REST query.
[<Interface>]
type IRestQueryFilter =
  /// <summary>
  /// Filters generic queryable using provided REST query.
  /// </summary>
  /// <typeparam name="a">Element type of the queryable.</typeparam>
  /// <param name="queryable">Source queryable.</param>
  /// <param name="restQuery">REST query to apply.</param>
  /// <returns>Filtered queryable.</returns>
  abstract FilterQuery : queryable:IQueryable<'a> * restQuery:RestQuery -> IQueryable<'a>

/// <summary>
/// Defines functionality for filtering queryable of concrete type based on REST query.
/// </summary>
/// <typeparam name="a">Element type of the queryable.</typeparam>
[<Interface>]
type IRestQueryFilter<'a> =
  /// <summary>
  /// Filters queryable using provided REST query.
  /// </summary>
  /// <param name="queryable">Source queryable.</param>
  /// <param name="restQuery">REST query to apply.</param>
  /// <returns>Filtered queryable.</returns>
  abstract FilterQuery : queryable:IQueryable<'a> * restQuery:RestQuery -> IQueryable<'a>

/// Defines functionality for ordering generic queryable based on REST query.
[<Interface>]
type IRestQueryOrderer =
  /// <summary>
  /// Orders generic queryable using provided REST query.
  /// </summary>
  /// <typeparam name="a">Element type of the queryable.</typeparam>
  /// <param name="queryable">Source queryable.</param>
  /// <param name="restQuery">REST query to apply.</param>
  /// <returns>Ordered queryable.</returns>
  abstract OrderQuery : queryable:IQueryable<'a> * restQuery:RestQuery -> IOrderedQueryable<'a>

/// <summary>
/// Defines functionality for ordering queryable of concrete type based on REST query.
/// </summary>
/// <typeparam name="a">Element type of the queryable.</typeparam>
[<Interface>]
type IRestQueryOrderer<'a> =
  /// <summary>
  /// Orders queryable using provided REST query.
  /// </summary>
  /// <param name="queryable">Source queryable.</param>
  /// <param name="restQuery">REST query to apply.</param>
  /// <returns>Ordered queryable.</returns>
  abstract OrderQuery : queryable:IQueryable<'a> * restQuery:RestQuery -> IOrderedQueryable<'a>

/// <summary>
/// Defines functionality for deserializing generic objects.
/// </summary>
[<Interface>]
type IDeserializer =
  /// <summary>
  /// Deserializes object of the specified type from stream.
  /// </summary>
  /// <typeparam name="a">Type of the object to deserialize.</typeparam>
  /// <param name="stream">Stream to deserialize object from.</param>
  /// <returns>Deserialized object.</returns>
  abstract Deserialize<'a> : stream:Stream -> 'a

/// <summary>
/// Defines functionality for deserializing objects of concrete type.
/// </summary>
/// <typeparam name="a">Type of the object to deserialize.</typeparam>
[<Interface>]
type IDeserializer<'a> =
  /// <summary>
  /// Deserializes object of the predefined type from stream.
  /// </summary>
  /// <param name="stream">Stream to deserialize object from.</param>
  /// <returns>Deserialized object.</returns>
  abstract Deserialize : stream:Stream -> 'a

/// <summary>
/// Defines functionality for serializing generic objects.
/// </summary>
[<Interface>]
type ISerializer =
  /// <summary>
  /// Serializes object of the specified type to configurable output.
  /// </summary>
  /// <typeparam name="a">Type of the object to serialize.</typeparam>
  /// <param name="stream">Configurable stream output to serialize to.</param>
  /// <param name="item">Object to serialize to.</param>
  abstract AsyncSerialize<'a> : output:IConfigurableOutput<Stream> * item:'a -> Async<unit>

/// <summary>
/// Defines functionality for serializing objects of concrete.
/// </summary>
/// <typeparam name="a">Type of the object to serialize.</typeparam>
[<Interface>]
type ISerializer<'a> =
  /// <summary>
  /// Serializes object of the predefined type to configurable output.
  /// </summary>
  /// <param name="stream">Configurable stream output to serialize to.</param>
  /// <param name="item">Object to serialize to.</param>
  abstract AsyncSerialize : stream:IConfigurableOutput<Stream> * item:'a -> Async<unit>

/// <summary>
/// Defines functionality for retrieving default ordering property from generic type.
/// </summary>
[<Interface>]
type IDefaultOrderProperty =
  /// <summary>
  /// Retrieves default ordering property for the specified generic type.
  /// </summary>
  /// <typeparam name="a">Type of the object to retrieve default ordering property for.</typeparam>
  /// <returns>Order by property descriptor.</returns>
  abstract Select<'a> : unit -> OrderByProperty

/// <summary>
/// Defines functionality for retrieving default ordering property from concrete type.
/// </summary>
[<Interface>]
type IDefaultOrderProperty<'a> =
  /// <summary>
  /// Retrieves default ordering property for the predefined type.
  /// </summary>
  /// <returns>Order by property descriptor.</returns>
  abstract Select : unit -> OrderByProperty

// REST methods

/// <summary>
/// Defines functionality to customize transactions used within trasnactional REST methods.
/// </summary>
type IRestTrasactedMethod =
  /// <summary>
  /// Initiates transaction required to perform transactional operation.
  /// </summary>
  /// <returns>Transaction to use.</returns>
  abstract AsyncBeginTransaction : unit -> Async<IDataTransaction>

/// <summary>
/// Defines functionality to implement generic REST ITEM method.
/// </summary>
[<Interface>]
type IRestItem =
  /// <summary>
  /// Performes REST ITEM action for the specified type.
  /// </summary>
  /// <typeparam name="a">Type of the target object.</typeparam>
  /// <typeparam name="id">Type of the Id property of the target object.</typeparam>
  /// <param name="id">Id of the object to return.</param>
  /// <returns>
  /// Object of the specified type for the specified id.
  /// </returns>
  abstract AsyncInvoke<'a, 'id when 'a :> IHasId<'id>> : id:'id -> Async<'a>

/// <summary>
/// Defines functionality to implement REST ITEM method for concrete type.
/// </summary>
/// <typeparam name="a">Type of the target object.</typeparam>
/// <typeparam name="id">Type of the Id property of the target object.</typeparam>
[<Interface>]
type IRestItem<'a, 'id when 'a :> IHasId<'id> and 'id : equality> =
  /// <summary>
  /// Performes REST ITEM action for the predefined type.
  /// </summary>
  /// <param name="id">Id of the object to return.</param>
  /// <returns>
  /// Object of the specified type for the specified id.
  /// </returns>
  abstract AsyncInvoke : id:'id -> Async<'a>

/// <summary>
/// Defines functionality to implement generic REST CREATE method.
/// </summary>
[<Interface>]
type IRestCreate =
  inherit IRestTrasactedMethod
  /// <summary>
  /// Performes REST CREATE action for the specified type.
  /// </summary>
  /// <typeparam name="a">Type of the target object.</typeparam>
  /// <typeparam name="id">Type of the Id property of the target object.</typeparam>
  /// <param name="data">Object to insert into dataset.</param>
  /// <returns>
  /// Object returned by dataset after insert operation. Depending on the dataset implementation some of the values of
  /// the returned object may differ from the input.
  /// </returns>
  abstract AsyncInvoke<'a, 'id when 'a :> IHasId<'id> and 'id : equality> : data:'a -> Async<'a>

/// <summary>
/// Defines functionality to implement REST CREATE method for the concrete type.
/// </summary>
/// <typeparam name="a">Type of the target object.</typeparam>
/// <typeparam name="id">Type of the Id property of the target object.</typeparam>
[<Interface>]
type IRestCreate<'a, 'id when 'a :> IHasId<'id> and 'id : equality> =
  inherit IRestTrasactedMethod
  /// <summary>
  /// Performes REST CREATE action for the predefined type.
  /// </summary>
  /// <param name="data">Object to insert into dataset.</param>
  /// <returns>
  /// Object returned by dataset after insert operation. Depending on the dataset implementation some of the values of
  /// the returned object may differ from the input.
  /// </returns>
  abstract AsyncInvoke : data:'a -> Async<'a>

/// <summary>
/// Defines functionality to implement generic REST UPDATE method.
/// </summary>
[<Interface>]
type IRestUpdate =
  inherit IRestTrasactedMethod
  /// <summary>
  /// Performes REST UPDATE action for the specified type.
  /// </summary>
  /// <typeparam name="a">Type of the target object.</typeparam>
  /// <typeparam name="id">Type of the Id property of the target object.</typeparam>
  /// <param name="id">Id of the object to update.</param>
  /// <param name="data">Object to update in dataset.</param>
  /// <returns>
  /// Object returned by dataset after update operation. Depending on the dataset implementation some of the values of
  /// the returned object may differ from the input.
  /// </returns>
  abstract AsyncInvoke<'a, 'id when 'a :> IHasId<'id> and 'id : equality> : id:'id * data:'a -> Async<'a>

/// <summary>
/// Defines functionality to implement REST UPDATE method for the concrete type.
/// </summary>
/// <typeparam name="a">Type of the target object.</typeparam>
/// <typeparam name="id">Type of the Id property of the target object.</typeparam>
[<Interface>]
type IRestUpdate<'a, 'id when 'a :> IHasId<'id> and 'id : equality> =
  inherit IRestTrasactedMethod
  /// <summary>
  /// Performes REST UPDATE action for the predefined type.
  /// </summary>
  /// <param name="id">Id of the object to update.</param>
  /// <param name="data">Object to update in dataset.</param>
  /// <returns>
  /// Object returned by dataset after update operation. Depending on the dataset implementation some of the values of
  /// the returned object may differ from the input.
  /// </returns>
  abstract AsyncInvoke : id:'id * data:'a -> Async<'a>

/// <summary>
/// Defines functionality to implement generic REST DELETE method.
/// </summary>
[<Interface>]
type IRestDelete =
  inherit IRestTrasactedMethod
  /// <summary>
  /// Performes REST UPDATE action for the specified type.
  /// </summary>
  /// <typeparam name="a">Type of the target object.</typeparam>
  /// <typeparam name="id">Type of the Id property of the target object.</typeparam>
  /// <param name="id">Id of the object to delete.</param>
  abstract AsyncInvoke<'a, 'id when 'a :> IHasId<'id> and 'id : equality> : id:'id -> Async<unit>

/// <summary>
/// Defines functionality to implement REST DELETE method for the concrete type.
/// </summary>
/// <typeparam name="a">Type of the target object.</typeparam>
/// <typeparam name="id">Type of the Id property of the target object.</typeparam>
[<Interface>]
type IRestDelete<'a, 'id when 'a :> IHasId<'id> and 'id : equality> =
  inherit IRestTrasactedMethod
  /// <summary>
  /// Performes REST DELETE action for the predefined type.
  /// </summary>
  /// <param name="id">Id of the object to delete.</param>
  abstract AsyncInvoke : id:'id -> Async<unit>

/// <summary>
/// Defines functionality to implement generic REST LIST method.
/// </summary>
[<Interface>]
type IRestListCollection =
  /// <summary>
  /// Performes REST LIST action for the specified type with the specified parameters.
  /// </summary>
  /// <typeparam name="a">Type of the target object.</typeparam>
  /// <param name="restQuery">Query options specified in the request.</param>
  /// <param name="accessValidator">
  /// Queryable decorator that filters out non-accessible entities depending on the access configuration.
  /// </param>
  /// <returns>
  /// REST LIST response that contains partial resultset defined by Offset and Count properties of the rest query
  /// parameter, and total entity count for the conditions specified by the rest query parameter.
  /// </returns>
  abstract AsyncInvoke<'a> : restQuery:RestQuery * accessValidator:(IQueryable -> Async<IQueryable>)  -> Async<struct ('a[] * int)>

/// <summary>
/// Defines functionality to implement REST DELETE method for the concrete type.
/// </summary>
/// <typeparam name="a">Type of the target object.</typeparam>
[<Interface>]
type IRestListCollection<'a> =
  /// <summary>
  /// Performes REST LIST action for the predefined type with the specified parameters.
  /// </summary>
  /// <param name="restQuery">Query options specified in the request.</param>
  /// <param name="accessValidator">
  /// Queryable decorator that filters out non-accessible entities depending on the access configuration.
  /// </param>
  /// <returns>
  /// REST LIST response that contains partial resultset defined by Offset and Count properties of the rest query
  /// parameter, and total entity count for the conditions specified by the rest query parameter.
  /// </returns>
  abstract AsyncInvoke : restQuery:RestQuery * accessValidator:(IQueryable -> Async<IQueryable>) -> Async<struct ('a[] * int)>

// **************************************************************************
// C# interop

[<AbstractClass>]
type AsyncRestMethodInvoker () =
  abstract InvokeAsync<'TResult>
    :  RestMethodInvocation<'TResult>
    *  [<Optional>] cancellationToken:CancellationToken
    -> Task<'TResult>
  member inline internal this.InvokeDirect<'TResult> (target, cancellationToken) = this.InvokeAsync<'TResult> (target, cancellationToken)
  interface IRestMethodInvoker with
    member this.AsyncInvoke target = Async.Adapt (fun cancellationToken -> this.InvokeAsync (target, cancellationToken))


/// <summary>
/// Represents generic object serializer.
/// </summary>
[<AbstractClass>]
type AsyncSerializer () =
  /// <summary>
  /// Serializes object of the specified type to configurable output.
  /// </summary>
  /// <typeparam name="a">Type of the object to serialize.</typeparam>
  /// <param name="stream">Configurable stream output to serialize to.</param>
  /// <param name="item">Object to serialize to.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  abstract SerializeAsync<'a> : stream:IConfigurableOutput<Stream> * item:'a * cancellationToken:CancellationToken -> Task
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.SerializeDirect<'a> (stream, item, cancellationToken) = this.SerializeAsync (stream, item, cancellationToken)
  interface ISerializer with
    member this.AsyncSerialize<'a> (stream, item) = Async.Adapt (fun cancellationToken -> this.SerializeAsync<'a> (stream, item, cancellationToken))

/// <summary>
/// Represents object serializer for the concrete type.
/// </summary>
[<AbstractClass>]
type AsyncSerializer<'a> () =
  /// <summary>
  /// Serializes object of the predefined type to configurable output.
  /// </summary>
  /// <param name="stream">Configurable stream output to serialize to.</param>
  /// <param name="item">Object to serialize to.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  abstract SerializeAsync : stream:IConfigurableOutput<Stream> * item:'a * cancellationToken:CancellationToken -> Task
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.SerializeDirect (stream, item, cancellationToken) = this.SerializeAsync (stream, item, cancellationToken)
  interface ISerializer<'a> with
    member this.AsyncSerialize (stream, item) = Async.Adapt (fun cancellationToken -> this.SerializeAsync (stream, item, cancellationToken))

/// <summary>
/// Represents REST ITEM method implmenetation for the concrete type.
/// </summary>
[<AbstractClass>]
type RestItem<'a, 'id when 'a :> IHasId<'id> and 'id : equality> () =
  /// <summary>
  /// Performes REST ITEM action for the predefined type.
  /// </summary>
  /// <param name="id">Id of the object to return.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>
  /// Object of the specified type for the specified id.
  /// </returns>
  abstract InvokeAsync : id:'id * cancellationToken:CancellationToken -> Task<'a>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.InvokeDirect (id, cancellationToken) = this.InvokeAsync (id, cancellationToken)
  interface IRestItem<'a, 'id> with
    member this.AsyncInvoke id = Async.Adapt (fun cancellationToken -> this.InvokeAsync (id, cancellationToken))

/// <summary>
/// Base class for transacted REST method implmentations.
/// </summary>
[<AbstractClass>]
type RestTransactedMethod () =
  /// <summary>
  /// Initiates transaction required to perform insert operation.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Transaction to use.</returns>
  abstract BeginTransactionAsync : cancellationToken:CancellationToken -> Task<IDataTransaction>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.BeginTransactionDirect cancellationToken = this.BeginTransactionAsync cancellationToken
  interface IRestTrasactedMethod with
    member this.AsyncBeginTransaction () = Async.Adapt this.BeginTransactionAsync

/// <summary>
/// Represents REST CREATE method implmenetation for the concrete type.
/// </summary>
[<AbstractClass>]
type RestCreate<'a, 'id when 'a :> IHasId<'id> and 'id : equality> () =
  inherit RestTransactedMethod ()
  /// <summary>
  /// Performes REST CREATE action for the predefined type.
  /// </summary>
  /// <param name="data">Object to insert into dataset.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>
  /// Object returned by dataset after insert operation. Depending on the dataset implementation some of the values of
  /// the returned object may differ from the input.
  /// </returns>
  abstract InvokeAsync : data:'a * cancellationToken:CancellationToken -> Task<'a>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.InvokeDirect (data, cancellationToken) = this.InvokeAsync (data, cancellationToken)
  interface IRestCreate<'a, 'id> with
    member this.AsyncInvoke data = Async.Adapt (fun cancellationToken -> this.InvokeAsync (data, cancellationToken))

/// <summary>
/// Represents REST UPDATE method implmenetation for the concrete type.
/// </summary>
[<AbstractClass>]
type RestUpdate<'a, 'id when 'a :> IHasId<'id> and 'id : equality> () =
  inherit RestTransactedMethod ()
  /// <summary>
  /// Performes REST UPDATE action for the predefined type.
  /// </summary>
  /// <param name="id">Id of the object to update.</param>
  /// <param name="data">Object to update in dataset.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>
  /// Object returned by dataset after update operation. Depending on the dataset implementation some of the values of
  /// the returned object may differ from the input.
  /// </returns>
  abstract InvokeAsync : id:'id * data:'a * cancellationToken:CancellationToken -> Task<'a>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.InvokeDirect (id, data, cancellationToken) = this.InvokeAsync (id, data, cancellationToken)
  interface IRestUpdate<'a, 'id> with
    member this.AsyncInvoke (id, data) = Async.Adapt (fun cancellationToken -> this.InvokeAsync (id, data, cancellationToken))

/// <summary>
/// Represents REST DELETE method implmenetation for the concrete type.
/// </summary>
[<AbstractClass>]
type RestDelete<'a, 'id when 'a :> IHasId<'id> and 'id : equality> () =
  inherit RestTransactedMethod ()
  /// <summary>
  /// Performes REST DELETE action for the predefined type.
  /// </summary>
  /// <param name="id">Id of the object to delete.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  abstract InvokeAsync : id:'id * cancellationToken:CancellationToken -> Task
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.InvokeDirect (id, cancellationToken) = this.InvokeAsync (id, cancellationToken)
  interface IRestDelete<'a, 'id> with
    member this.AsyncInvoke id = Async.Adapt (fun cancellationToken -> this.InvokeAsync (id, cancellationToken))

/// <summary>
/// Represents REST LIST method implmenetation for the concrete type.
/// </summary>
[<AbstractClass>]
type RestListCollection<'a> () =
  /// <summary>
  /// Performes REST LIST action for the predefined type with the specified parameters.
  /// </summary>
  /// <param name="restQuery">Query options specified in the request.</param>
  /// <param name="accessValidator">
  /// Queryable decorator that filters out non-accessible entities depending on the access configuration.
  /// </param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>
  /// REST LIST response that contains partial resultset defined by Offset and Count properties of the rest query
  /// parameter, and total entity count for the conditions specified by the rest query parameter.
  /// </returns>
  abstract InvokeAsync : restQuery:RestQuery * accessValidator:Func<IQueryable, CancellationToken, Task<IQueryable>> * cancellationToken:CancellationToken -> Task<struct ('a[] * int)>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.InvokeDirect (restQuery, accessValidator, cancellationToken) = this.InvokeAsync (restQuery, accessValidator, cancellationToken)
  interface IRestListCollection<'a> with
    member this.AsyncInvoke (restQuery, accessValidator) =
      let adaptedAccessValidator =
        Func<IQueryable, CancellationToken, Task<IQueryable>>
          (fun queryable cancellationToken ->
            Async.StartAsTask (accessValidator queryable, cancellationToken = cancellationToken))
      Async.Adapt (fun cancellationToken -> this.InvokeAsync (restQuery, adaptedAccessValidator, cancellationToken))

/// <summary>
/// Contains extesions for REST related objects.
/// </summary>
[<Extension>]
[<AbstractClass; Sealed>]
type RestContractExtensions =

  /// <summary>
  /// Instantiates query filter for concrete type from generic query filter.
  /// </summary>
  /// <typeparam name="a">Target element type.</typeparam>
  /// <param name="this">Instance.</param>
  /// <returns>Query filter for requested type.</returns>
  [<Extension>]
  static member For<'a>(this : IRestQueryFilter) =
    { new IRestQueryFilter<'a> with
        member __.FilterQuery (queryable, restQuery) = this.FilterQuery (queryable, restQuery)
    }

  /// <summary>
  /// Instantiates query orderer for concrete type from generic query orderer.
  /// </summary>
  /// <typeparam name="a">Target element type.</typeparam>
  /// <param name="this">Instance.</param>
  /// <returns>Query orderer for requested type.</returns>
  [<Extension>]
  static member For<'a>(this : IRestQueryOrderer) =
    { new IRestQueryOrderer<'a> with
        member __.OrderQuery (queryable, restQuery) = this.OrderQuery (queryable, restQuery)
    }

  /// <summary>
  /// Instantiates deserializer for concrete type from generic deserializer.
  /// </summary>
  /// <typeparam name="a">Target object type.</typeparam>
  /// <param name="this">Instance.</param>
  /// <returns>Deserializer for requested type.</returns>
  [<Extension>]
  static member For<'a>(this : IDeserializer) =
    { new IDeserializer<'a> with
        member __.Deserialize stream = this.Deserialize stream
    }

  /// <summary>
  /// Instantiates serializer for concrete type from generic deserializer.
  /// </summary>
  /// <typeparam name="a">Target object type.</typeparam>
  /// <param name="this">Instance.</param>
  /// <returns>Serializer for requested type.</returns>
  [<Extension>]
  static member For<'a>(this : ISerializer) =
    { new ISerializer<'a> with
        member __.AsyncSerialize (stream, item) = this.AsyncSerialize (stream, item)
    }

  /// <summary>
  /// Instantiates default ordering property selector for concrete type from generic default ordering property selector.
  /// </summary>
  /// <typeparam name="a">Target object type.</typeparam>
  /// <param name="this">Instance.</param>
  /// <returns>Default ordering property selector for requested type.</returns>
  [<Extension>]
  static member For<'a>(this : IDefaultOrderProperty) =
    { new IDefaultOrderProperty<'a> with
        member __.Select () = this.Select<'a> ()
    }

  /// <summary>
  /// Instantiates REST ITEM implementation for concrete type from generic REST ITEM implementation.
  /// </summary>
  /// <typeparam name="a">Target item type.</typeparam>
  /// <typeparam name="id">Type of id of the target item type.</typeparam>
  /// <param name="this">Instance.</param>
  /// <returns>REST ITEM implementation for requested type.</returns>
  [<Extension>]
  static member For<'a, 'id when 'a :> IHasId<'id> and 'id : equality>(this : IRestItem) =
    { new IRestItem<'a, 'id> with
        member __.AsyncInvoke id = this.AsyncInvoke id
    }

  /// <summary>
  /// Instantiates REST CREATE implementation for concrete type from generic REST ITEM implementation.
  /// </summary>
  /// <typeparam name="a">Target item type.</typeparam>
  /// <typeparam name="id">Type of id of the target item type.</typeparam>
  /// <param name="this">Instance.</param>
  /// <returns>REST CREATE implementation for requested type.</returns>
  [<Extension>]
  static member For<'a, 'id when 'a :> IHasId<'id> and 'id : equality>(this : IRestCreate) =
    { new IRestCreate<'a, 'id> with
        member __.AsyncBeginTransaction () = this.AsyncBeginTransaction ()
        member __.AsyncInvoke data = this.AsyncInvoke data
    }

  /// <summary>
  /// Instantiates REST UPDATE implementation for concrete type from generic REST ITEM implementation.
  /// </summary>
  /// <typeparam name="a">Target item type.</typeparam>
  /// <typeparam name="id">Type of id of the target item type.</typeparam>
  /// <param name="this">Instance.</param>
  /// <returns>REST UPDATE implementation for requested type.</returns>
  [<Extension>]
  static member For<'a, 'id when 'a :> IHasId<'id> and 'id : equality>(this : IRestUpdate) =
    { new IRestUpdate<'a, 'id> with
        member __.AsyncBeginTransaction () = this.AsyncBeginTransaction ()
        member __.AsyncInvoke (id, data)  = this.AsyncInvoke (id, data)
    }

  /// <summary>
  /// Instantiates REST DELETE implementation for concrete type from generic REST ITEM implementation.
  /// </summary>
  /// <typeparam name="a">Target item type.</typeparam>
  /// <typeparam name="id">Type of id of the target item type.</typeparam>
  /// <param name="this">Instance.</param>
  /// <returns>REST DELETE implementation for requested type.</returns>
  [<Extension>]
  static member For<'a, 'id when 'a :> IHasId<'id> and 'id : equality>(this : IRestDelete) =
    { new IRestDelete<'a, 'id> with
        member __.AsyncBeginTransaction () = this.AsyncBeginTransaction ()
        member __.AsyncInvoke id = this.AsyncInvoke id
    }

  /// <summary>
  /// Instantiates REST LIST implementation for concrete type from generic REST ITEM implementation.
  /// </summary>
  /// <typeparam name="a">Target item type.</typeparam>
  /// <param name="this">Instance.</param>
  /// <returns>REST LIST implementation for requested type.</returns>
  [<Extension>]
  static member For<'a>(this : IRestListCollection) =
    { new IRestListCollection<'a> with
        member __.AsyncInvoke (restQuery, accessValidator) = this.AsyncInvoke (restQuery, accessValidator)
    }

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member InvokeAsync (this : RestMethodInvocation<_>, [<Optional>] cancellationToken) =
    Async.StartAsTask (this.AsyncInvoke (), cancellationToken = cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member InvokeAsync (this : IRestMethodInvoker, target : RestMethodInvocation<_>, [<Optional>] cancellationToken) =
    match this with
    | :? AsyncRestMethodInvoker as inst -> inst.InvokeDirect (target, cancellationToken)
    | _                                 -> Async.StartAsTask (this.AsyncInvoke target, cancellationToken = cancellationToken)

  /// <summary>
  /// Serializes object of the predefined type to configurable output.
  /// </summary>
  /// <param name="this">Instance.</param>
  /// <param name="stream">Configurable stream output to serialize to.</param>
  /// <param name="item">Object to serialize to.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member SerializeAsync (this : ISerializer<'a>, stream, item, cancellationToken) =
    match this with
    | :? AsyncSerializer<'a> as inst -> inst.SerializeDirect (stream, item, cancellationToken)
    | _                              -> Async.StartAsTask (this.AsyncSerialize (stream, item), cancellationToken = cancellationToken) :> _

  /// <summary>
  /// Performes REST ITEM action for the predefined type.
  /// </summary>
  /// <param name="this">Instance.</param>
  /// <param name="id">Id of the object to return.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>
  /// Object of the specified type for the specified id.
  /// </returns>
  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member InvokeAsync (this : IRestItem<'a, 'id>, id, cancellationToken) =
    match this with
    | :? RestItem<'a, 'id> as inst -> inst.InvokeDirect (id, cancellationToken)
    | _                            -> Async.StartAsTask (this.AsyncInvoke id, cancellationToken = cancellationToken)

  /// <summary>
  /// Performes REST CREATE action for the predefined type.
  /// </summary>
  /// <param name="this">Instance.</param>
  /// <param name="data">Object to insert into dataset.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>
  /// Object returned by dataset after insert operation. Depending on the dataset implementation some of the values of
  /// the returned object may differ from the input.
  /// </returns>
  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member InvokeAsync (this : IRestCreate<'a, 'id>, data, cancellationToken) =
    match this with
    | :? RestCreate<'a, 'id> as inst -> inst.InvokeDirect (data, cancellationToken)
    | _                              -> Async.StartAsTask (this.AsyncInvoke data, cancellationToken = cancellationToken)

  /// <summary>
  /// Performes REST UPDATE action for the predefined type.
  /// </summary>
  /// <param name="this">Instance.</param>
  /// <param name="id">Id of the object to update.</param>
  /// <param name="data">Object to update in dataset.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>
  /// Object returned by dataset after update operation. Depending on the dataset implementation some of the values of
  /// the returned object may differ from the input.
  /// </returns>
  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member InvokeAsync (this : IRestUpdate<'a, 'id>, id, data, cancellationToken) =
    match this with
    | :? RestUpdate<'a, 'id> as inst -> inst.InvokeDirect (id, data, cancellationToken)
    | _                              -> Async.StartAsTask (this.AsyncInvoke (id, data), cancellationToken = cancellationToken)

  /// <summary>
  /// Performes REST DELETE action for the predefined type.
  /// </summary>
  /// <param name="this">Instance.</param>
  /// <param name="id">Id of the object to delete.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member InvokeAsync (this : IRestDelete<'a, 'id>, id, cancellationToken) =
    match this with
    | :? RestDelete<'a, 'id> as inst -> inst.InvokeDirect (id, cancellationToken)
    | _                              -> Async.StartAsTask (this.AsyncInvoke id, cancellationToken = cancellationToken) :> Task

  /// <summary>
  /// Performes REST LIST action for the predefined type with the specified parameters.
  /// </summary>
  /// <param name="this">Instance.</param>
  /// <param name="restQuery">Query options specified in the request.</param>
  /// <param name="accessValidator">
  /// Queryable decorator that filters out non-accessible entities depending on the access configuration.
  /// </param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>
  /// REST LIST response that contains partial resultset defined by Offset and Count properties of the rest query
  /// parameter, and total entity count for the conditions specified by the rest query parameter.
  /// </returns>
  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member InvokeAsync (this : IRestListCollection<'a>, accessValidator, restQuery, cancellationToken) =
    match this with
    | :? RestListCollection<'a> as inst -> inst.InvokeDirect (restQuery, accessValidator, cancellationToken)
    | _                                 ->
      let adaptedAccessValidator queryable =
        Async.Adapt (fun cancellationToken -> accessValidator.Invoke (queryable, cancellationToken))
      Async.StartAsTask (this.AsyncInvoke (restQuery, adaptedAccessValidator), cancellationToken = cancellationToken)

  /// <summary>
  /// Initiates transaction required to perform insert operation.
  /// </summary>
  /// <param name="this">Instance.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Transaction to use.</returns>
  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member BeginTransactionAsync (this : IRestTrasactedMethod, cancellationToken) =
    match this with
    | :? RestTransactedMethod as inst -> inst.BeginTransactionDirect cancellationToken
    | _                               -> Async.StartAsTask (this.AsyncBeginTransaction (), cancellationToken = cancellationToken)
