namespace NCoreUtils.AspNetCore.Rest

open System.Linq
open System.IO
open System.Reflection
open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Tasks
open NCoreUtils
open NCoreUtils.Data
open System

type IRestQueryFilter =
  abstract FilterQuery : queryable:IQueryable<'a> * restQuery:RestQuery -> IQueryable<'a>

type IRestQueryFilter<'a> =
  abstract FilterQuery : queryable:IQueryable<'a> * restQuery:RestQuery -> IQueryable<'a>

type IRestQueryOrderer =
  abstract OrderQuery : queryable:IQueryable<'a> * restQuery:RestQuery -> IOrderedQueryable<'a>

type IRestQueryOrderer<'a> =
  abstract OrderQuery : queryable:IQueryable<'a> * restQuery:RestQuery -> IOrderedQueryable<'a>

type IDeserializer =
  abstract Deserialize<'a> : stream:Stream -> 'a

type IDeserializer<'a> =
  abstract Deserialize : stream:Stream -> 'a

type ISerializer =
  abstract AsyncSerialize<'a> : stream:IConfigurableOutput<Stream> * item:'a -> Async<unit>

type ISerializer<'a> =
  abstract AsyncSerialize : stream:IConfigurableOutput<Stream> * item:'a -> Async<unit>

type IDefaultOrderProperty =
  abstract Select<'a> : unit -> struct (PropertyInfo * bool)

type IDefaultOrderProperty<'a> =
  abstract Select : unit -> struct (PropertyInfo * bool)

// REST methods

type IRestItem =
  abstract AsyncInvoke<'a, 'id when 'a :> IHasId<'id>> : id:'id -> Async<'a>

type IRestItem<'a, 'id when 'a :> IHasId<'id> and 'id : equality> =
  abstract AsyncInvoke : id:'id -> Async<'a>

type IRestCreate =
  abstract AsyncBeginTransaction : unit -> Async<IDataTransaction>
  abstract AsyncInvoke<'a, 'id when 'a :> IHasId<'id> and 'id : equality> : data:'a -> Async<'a>

type IRestCreate<'a, 'id when 'a :> IHasId<'id> and 'id : equality> =
  abstract AsyncBeginTransaction : unit -> Async<IDataTransaction>
  abstract AsyncInvoke : data:'a -> Async<'a>

type IRestUpdate =
  abstract AsyncBeginTransaction : unit -> Async<IDataTransaction>
  abstract AsyncInvoke<'a, 'id when 'a :> IHasId<'id> and 'id : equality> : id:'id * data:'a -> Async<'a>

type IRestUpdate<'a, 'id when 'a :> IHasId<'id> and 'id : equality> =
  abstract AsyncBeginTransaction : unit -> Async<IDataTransaction>
  abstract AsyncInvoke : id:'id * data:'a -> Async<'a>

type IRestDelete =
  abstract AsyncBeginTransaction : unit -> Async<IDataTransaction>
  abstract AsyncInvoke<'a, 'id when 'a :> IHasId<'id> and 'id : equality> : id:'id -> Async<unit>

type IRestDelete<'a, 'id when 'a :> IHasId<'id> and 'id : equality> =
  abstract AsyncBeginTransaction : unit -> Async<IDataTransaction>
  abstract AsyncInvoke : id:'id -> Async<unit>

type IRestListCollection =
  abstract AsyncInvoke<'a> : restQuery:RestQuery * accessValidator:(IQueryable -> Async<IQueryable>)  -> Async<struct ('a[] * int)>

type IRestListCollection<'a> =
  abstract AsyncInvoke : restQuery:RestQuery * accessValidator:(IQueryable -> Async<IQueryable>) -> Async<struct ('a[] * int)>

// **************************************************************************
// C# interop

[<AbstractClass>]
type AsyncSerializer () =
  abstract SerializeAsync<'a> : stream:IConfigurableOutput<Stream> * item:'a * cancellationToken:CancellationToken -> Task
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.SerializeDirect<'a> (stream, item, cancellationToken) = this.SerializeAsync (stream, item, cancellationToken)
  interface ISerializer with
    member this.AsyncSerialize<'a> (stream, item) = Async.Adapt (fun cancellationToken -> this.SerializeAsync<'a> (stream, item, cancellationToken))

[<AbstractClass>]
type AsyncSerializer<'a> () =
  abstract SerializeAsync : stream:IConfigurableOutput<Stream> * item:'a * cancellationToken:CancellationToken -> Task
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.SerializeDirect (stream, item, cancellationToken) = this.SerializeAsync (stream, item, cancellationToken)
  interface ISerializer<'a> with
    member this.AsyncSerialize (stream, item) = Async.Adapt (fun cancellationToken -> this.SerializeAsync (stream, item, cancellationToken))

[<AbstractClass>]
type RestItem<'a, 'id when 'a :> IHasId<'id> and 'id : equality> () =
  abstract InvokeAsync : id:'id * cancellationToken:CancellationToken -> Task<'a>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.InvokeDirect (id, cancellationToken) = this.InvokeAsync (id, cancellationToken)
  interface IRestItem<'a, 'id> with
    member this.AsyncInvoke id = Async.Adapt (fun cancellationToken -> this.InvokeAsync (id, cancellationToken))

[<AbstractClass>]
type RestTransactedMethod () =
  abstract BeginTransactionAsync : cancellationToken:CancellationToken -> Task<IDataTransaction>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.BeginTransactionDirect cancellationToken = this.BeginTransactionAsync cancellationToken

[<AbstractClass>]
type RestCreate<'a, 'id when 'a :> IHasId<'id> and 'id : equality> () =
  inherit RestTransactedMethod ()
  abstract InvokeAsync : data:'a * cancellationToken:CancellationToken -> Task<'a>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.InvokeDirect (data, cancellationToken) = this.InvokeAsync (data, cancellationToken)
  interface IRestCreate<'a, 'id> with
    member this.AsyncBeginTransaction () = Async.Adapt this.BeginTransactionAsync
    member this.AsyncInvoke data = Async.Adapt (fun cancellationToken -> this.InvokeAsync (data, cancellationToken))

[<AbstractClass>]
type RestUpdate<'a, 'id when 'a :> IHasId<'id> and 'id : equality> () =
  inherit RestTransactedMethod ()
  abstract InvokeAsync : id:'id * data:'a * cancellationToken:CancellationToken -> Task<'a>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.InvokeDirect (id, data, cancellationToken) = this.InvokeAsync (id, data, cancellationToken)
  interface IRestUpdate<'a, 'id> with
    member this.AsyncBeginTransaction () = Async.Adapt this.BeginTransactionAsync
    member this.AsyncInvoke (id, data) = Async.Adapt (fun cancellationToken -> this.InvokeAsync (id, data, cancellationToken))

[<AbstractClass>]
type RestDelete<'a, 'id when 'a :> IHasId<'id> and 'id : equality> () =
  inherit RestTransactedMethod ()
  abstract InvokeAsync : id:'id * cancellationToken:CancellationToken -> Task
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.InvokeDirect (id, cancellationToken) = this.InvokeAsync (id, cancellationToken)
  interface IRestDelete<'a, 'id> with
    member this.AsyncBeginTransaction () = Async.Adapt this.BeginTransactionAsync
    member this.AsyncInvoke id = Async.Adapt (fun cancellationToken -> this.InvokeAsync (id, cancellationToken))

[<AbstractClass>]
type RestListCollection<'a> () =
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

[<Extension>]
[<AbstractClass; Sealed>]
type RestContractExtensions =

  [<Extension>]
  static member For<'a>(this : IRestQueryFilter) =
    { new IRestQueryFilter<'a> with
        member __.FilterQuery (queryable, restQuery) = this.FilterQuery (queryable, restQuery)
    }

  [<Extension>]
  static member For<'a>(this : IRestQueryOrderer) =
    { new IRestQueryOrderer<'a> with
        member __.OrderQuery (queryable, restQuery) = this.OrderQuery (queryable, restQuery)
    }

  [<Extension>]
  static member For<'a>(this : IDeserializer) =
    { new IDeserializer<'a> with
        member __.Deserialize stream = this.Deserialize stream
    }

  [<Extension>]
  static member For<'a>(this : ISerializer) =
    { new ISerializer<'a> with
        member __.AsyncSerialize (stream, item) = this.AsyncSerialize (stream, item)
    }

  [<Extension>]
  static member For<'a>(this : IDefaultOrderProperty) =
    { new IDefaultOrderProperty<'a> with
        member __.Select () = this.Select<'a> ()
    }

  [<Extension>]
  static member For<'a, 'id when 'a :> IHasId<'id> and 'id : equality>(this : IRestItem) =
    { new IRestItem<'a, 'id> with
        member __.AsyncInvoke id = this.AsyncInvoke id
    }

  [<Extension>]
  static member For<'a, 'id when 'a :> IHasId<'id> and 'id : equality>(this : IRestCreate) =
    { new IRestCreate<'a, 'id> with
        member __.AsyncBeginTransaction () = this.AsyncBeginTransaction ()
        member __.AsyncInvoke data = this.AsyncInvoke data
    }

  [<Extension>]
  static member For<'a, 'id when 'a :> IHasId<'id> and 'id : equality>(this : IRestUpdate) =
    { new IRestUpdate<'a, 'id> with
        member __.AsyncBeginTransaction () = this.AsyncBeginTransaction ()
        member __.AsyncInvoke (id, data)  = this.AsyncInvoke (id, data)
    }

  [<Extension>]
  static member For<'a, 'id when 'a :> IHasId<'id> and 'id : equality>(this : IRestDelete) =
    { new IRestDelete<'a, 'id> with
        member __.AsyncBeginTransaction () = this.AsyncBeginTransaction ()
        member __.AsyncInvoke id = this.AsyncInvoke id
    }

  [<Extension>]
  static member For<'a>(this : IRestListCollection) =
    { new IRestListCollection<'a> with
        member __.AsyncInvoke (restQuery, accessValidator) = this.AsyncInvoke (restQuery, accessValidator)
    }

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member SerializeAsync (this : ISerializer<'a>, stream, item, cancellationToken) =
    match this with
    | :? AsyncSerializer<'a> as inst -> inst.SerializeDirect (stream, item, cancellationToken)
    | _                              -> Async.StartAsTask (this.AsyncSerialize (stream, item), cancellationToken = cancellationToken) :> _


  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member InvokeAsync (this : IRestItem<'a, 'id>, id, cancellationToken) =
    match this with
    | :? RestItem<'a, 'id> as inst -> inst.InvokeDirect (id, cancellationToken)
    | _                            -> Async.StartAsTask (this.AsyncInvoke id, cancellationToken = cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member InvokeAsync (this : IRestCreate<'a, 'id>, data, cancellationToken) =
    match this with
    | :? RestCreate<'a, 'id> as inst -> inst.InvokeDirect (data, cancellationToken)
    | _                              -> Async.StartAsTask (this.AsyncInvoke data, cancellationToken = cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member InvokeAsync (this : IRestUpdate<'a, 'id>, id, data, cancellationToken) =
    match this with
    | :? RestUpdate<'a, 'id> as inst -> inst.InvokeDirect (id, data, cancellationToken)
    | _                              -> Async.StartAsTask (this.AsyncInvoke (id, data), cancellationToken = cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member InvokeAsync (this : IRestDelete<'a, 'id>, id, cancellationToken) =
    match this with
    | :? RestDelete<'a, 'id> as inst -> inst.InvokeDirect (id, cancellationToken)
    | _                              -> Async.StartAsTask (this.AsyncInvoke id, cancellationToken = cancellationToken) :> Task

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member InvokeAsync (this : IRestListCollection<'a>, accessValidator, restQuery, cancellationToken) =
    match this with
    | :? RestListCollection<'a> as inst -> inst.InvokeDirect (restQuery, accessValidator, cancellationToken)
    | _                                 ->
      let adaptedAccessValidator queryable =
        Async.Adapt (fun cancellationToken -> accessValidator.Invoke (queryable, cancellationToken))
      Async.StartAsTask (this.AsyncInvoke (restQuery, adaptedAccessValidator), cancellationToken = cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member BeginTransactionAsync (this : IRestCreate<'a, 'id>, cancellationToken) =
    match this with
    | :? RestTransactedMethod as inst -> inst.BeginTransactionDirect cancellationToken
    | _                               -> Async.StartAsTask (this.AsyncBeginTransaction (), cancellationToken = cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member BeginTransactionAsync (this : IRestUpdate<'a, 'id>, cancellationToken) =
    match this with
    | :? RestTransactedMethod as inst -> inst.BeginTransactionDirect cancellationToken
    | _                               -> Async.StartAsTask (this.AsyncBeginTransaction (), cancellationToken = cancellationToken)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member BeginTransactionAsync (this : IRestDelete<'a, 'id>, cancellationToken) =
    match this with
    | :? RestTransactedMethod as inst -> inst.BeginTransactionDirect cancellationToken
    | _                               -> Async.StartAsTask (this.AsyncBeginTransaction (), cancellationToken = cancellationToken)
