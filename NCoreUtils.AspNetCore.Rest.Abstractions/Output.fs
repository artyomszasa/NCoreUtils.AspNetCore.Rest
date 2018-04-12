namespace NCoreUtils.AspNetCore.Rest

open System
open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Tasks
open NCoreUtils

[<CLIMutable>]
type OutputInfo = {
  Length      : Nullable<int64>
  ContentType : string }

module OutputInfo =

  [<CompiledName("Empty")>]
  let empty = { Length = Nullable.empty; ContentType = null }

type IConfigurableOutput<'a> =
  abstract AsyncInitialize : info:OutputInfo -> Async<'a>

// C# interop

[<AbstractClass>]
type AsyncConfigurableOutput<'a> () =
  abstract InitializeAsync : info:OutputInfo * cancellationToken:CancellationToken -> Task<'a>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.InitializeDirect (info, cancellationToken) = this.InitializeAsync (info, cancellationToken)
  interface IConfigurableOutput<'a> with
    member this.AsyncInitialize info = Async.Adapt (fun cancellationToken -> this.InitializeAsync (info, cancellationToken))

[<Extension>]
[<AbstractClass>]
type ConfigurableOutputExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member InitializeAsync<'a> (this : IConfigurableOutput<'a>, info, cancellationToken) =
    match this with
    | :? AsyncConfigurableOutput<'a> as inst -> inst.InitializeDirect (info, cancellationToken)
    | _                                      -> Async.StartAsTask (this.AsyncInitialize info, cancellationToken = cancellationToken)