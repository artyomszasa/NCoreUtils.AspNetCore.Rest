namespace NCoreUtils.AspNetCore.Rest

open System
open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Tasks
open NCoreUtils

/// <summary>
/// Provides optional information for configuring output.
/// </summary>
[<CLIMutable>]
type OutputInfo = {
  /// When set defines total length of the data that is to be outputed.
  Length      : Nullable<int64>
  /// When set defines content type of the data that is to be outputed.
  ContentType : string }

/// <summary>
/// Defines operations for optinal information for configuring output.
/// </summary>
module OutputInfo =

  /// Gets singleton optional information with no values set.
  [<CompiledName("Empty")>]
  let empty = { Length = Nullable.empty; ContentType = null }

/// <summary>
/// Defines functionality to implement configurable output of the specified type.
/// </summary>
/// <typeparam name="a">Type of the underlying output target.</typeparam>
type IConfigurableOutput<'a> =
  /// <summary>
  /// Initializes output with the spcified output information
  /// </summary>
  /// <param name="info">Optional output information.</param>
  /// <returns>Confugured output.</returns>
  abstract AsyncInitialize : info:OutputInfo -> Async<'a>

// C# interop

/// <summary>
/// Represents configurable output of the specified type.
/// </summary>
/// <typeparam name="a">Type of the underlying output target.</typeparam>
[<AbstractClass>]
type AsyncConfigurableOutput<'a> () =
  /// <summary>
  /// Initializes output with the spcified output information
  /// </summary>
  /// <param name="info">Optional output information.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Confugured output.</returns>
  abstract InitializeAsync : info:OutputInfo * cancellationToken:CancellationToken -> Task<'a>
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.InitializeDirect (info, cancellationToken) = this.InitializeAsync (info, cancellationToken)
  interface IConfigurableOutput<'a> with
    member this.AsyncInitialize info = Async.Adapt (fun cancellationToken -> this.InitializeAsync (info, cancellationToken))

/// Contains extensions for the configurable outputs.
[<Extension>]
[<AbstractClass>]
type ConfigurableOutputExtensions =

  /// <summary>
  /// Initializes output with the spcified output information
  /// </summary>
  /// <param name="this">Instance.</param>
  /// <param name="info">Optional output information.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Confugured output.</returns>
  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member InitializeAsync<'a> (this : IConfigurableOutput<'a>, info, cancellationToken) =
    match this with
    | :? AsyncConfigurableOutput<'a> as inst -> inst.InitializeDirect (info, cancellationToken)
    | _                                      -> Async.StartAsTask (this.AsyncInitialize info, cancellationToken = cancellationToken)