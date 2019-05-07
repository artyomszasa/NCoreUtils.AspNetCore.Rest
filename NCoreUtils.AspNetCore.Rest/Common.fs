[<AutoOpen>]
module internal NCoreUtils.AspNetCore.Rest.Common

open System
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Primitives
open NCoreUtils
open NCoreUtils.AspNetCore
open NCoreUtils.Data
open System.Collections.Concurrent
open System.Collections.Generic
open System.Diagnostics.CodeAnalysis
open System.Globalization
open System.Reflection
open System.Runtime.CompilerServices

[<ExcludeFromCodeCoverage>]
let inline idType ty =
  let mutable idTy = Unchecked.defaultof<_>
  match IdUtils.TryGetIdType (ty, &idTy) with
  | true -> idTy
  | _    -> invalidOpf "Type %s does not implement IHasId interface." ty.FullName

[<ExcludeFromCodeCoverage>]
let inline activate (ty : Type) = Activator.CreateInstance (ty, true)

[<RequiresExplicitTypeArguments>]
[<ExcludeFromCodeCoverage>]
let inline diActivate<'a> serviceProvider = ActivatorUtilities.CreateInstance<'a> serviceProvider

[<ExcludeFromCodeCoverage>]
let inline setResponseHeader (name : string) (value : string) httpContext =
  (HttpContext.response httpContext).Headers.Add (name, (StringValues : string -> _) value)

[<Sealed; AbstractClass>]
type internal Adapt =

  [<RequiresExplicitTypeArguments>]
  [<ExcludeFromCodeCoverage>]
  static member inline For<'a> (orderProperty : IDefaultOrderProperty) = orderProperty.For<'a> ()

  [<RequiresExplicitTypeArguments>]
  [<ExcludeFromCodeCoverage>]
  static member inline For<'a> (queryFilter : IRestQueryFilter) = queryFilter.For<'a> ()

  [<RequiresExplicitTypeArguments>]
  [<ExcludeFromCodeCoverage>]
  static member inline For<'a> (queryOrderer : IRestQueryOrderer) = queryOrderer.For<'a> ()

  [<RequiresExplicitTypeArguments>]
  [<ExcludeFromCodeCoverage>]
  static member inline For<'a> (deserializer : IDeserializer) = deserializer.For<'a> ()

  [<RequiresExplicitTypeArguments>]
  [<ExcludeFromCodeCoverage>]
  static member inline For<'a> (serializer : ISerializer) = serializer.For<'a> ()

  [<RequiresExplicitTypeArguments>]
  [<ExcludeFromCodeCoverage>]
  static member inline For<'a, 'id when 'a :> IHasId<'id> and 'id : equality> (item : IRestItem) = item.For<'a, 'id> ()

  [<RequiresExplicitTypeArguments>]
  [<ExcludeFromCodeCoverage>]
  static member inline For<'a, 'id when 'a :> IHasId<'id> and 'id : equality> (create : IRestCreate) = create.For<'a, 'id> ()

  [<RequiresExplicitTypeArguments>]
  [<ExcludeFromCodeCoverage>]
  static member inline For<'a, 'id when 'a :> IHasId<'id> and 'id : equality> (update : IRestUpdate) = update.For<'a, 'id> ()

  [<RequiresExplicitTypeArguments>]
  [<ExcludeFromCodeCoverage>]
  static member inline For<'a, 'id when 'a :> IHasId<'id> and 'id : equality> (delete : IRestDelete) = delete.For<'a, 'id> ()

  [<RequiresExplicitTypeArguments>]
  [<ExcludeFromCodeCoverage>]
  static member inline For<'a> (list : IRestListCollection) = list.For<'a> ()

[<Interface>]
type IBoxedInvoke<'TArg, 'TResult> =
  abstract Instance : obj
  abstract AsyncInvoke : arg:'TArg -> Async<'TResult>

[<Interface>]
type IBoxedInvoke<'TArg1, 'TArg2, 'TResult> =
  abstract Instance : obj
  abstract AsyncInvoke : arg1:'TArg1 * arg2:'TArg2 -> Async<'TResult>

[<AbstractClass>]
type RestMethodInvocation<'TItem, 'TResult> () =
  inherit RestMethodInvocation<'TResult> ()
  override __.ItemType = typeof<'TItem>

[<Sealed>]
type RestMethodInvocation<'TItem, 'TArg, 'TResult> (b : IBoxedInvoke<'TArg, 'TResult>, arg : 'TArg) =
  inherit RestMethodInvocation<'TItem, 'TResult> ()
  let arguments = [| box arg |] :> IReadOnlyList<_>
  override __.Arguments = arguments
  override __.Instance = b.Instance
  override __.AsyncInvoke () = b.AsyncInvoke (arg)
  override __.UpdateArguments newArguments =
    match newArguments.Count <> arguments.Count with
    | true -> invalidArgf "arguments" "arguments must contain 1 element, but is contains %d." newArguments.Count
    | _ ->
      let newArg =
        match newArguments.[0] with
        | :? 'TArg as arg -> arg
        | _               -> invalidOpf "Argument 0 is not compatible with type %A" typeof<'TArg>
      RestMethodInvocation<'TItem, 'TArg, 'TResult> (b, newArg) :> _

[<Sealed>]
type RestMethodInvocation<'TItem, 'TArg1, 'TArg2, 'TResult> (b : IBoxedInvoke<'TArg1, 'TArg2, 'TResult>, arg1 : 'TArg1, arg2 : 'TArg2) =
  inherit RestMethodInvocation<'TItem, 'TResult> ()
  let arguments = [| box arg1; box arg2 |] :> IReadOnlyList<_>
  override __.Arguments = arguments
  override __.Instance = b.Instance
  override __.AsyncInvoke () = b.AsyncInvoke (arg1, arg2)
  override __.UpdateArguments newArguments =
    match newArguments.Count <> arguments.Count with
    | true -> invalidArgf "arguments" "arguments must contain 2 elements, but is contains %d." newArguments.Count
    | _ ->
      let newArg1 =
        match newArguments.[0] with
        | :? 'TArg1 as arg -> arg
        | _                -> invalidOpf "Argument 0 is not compatible with type %A" typeof<'TArg1>
      let newArg2 =
        match newArguments.[1] with
        | :? 'TArg2 as arg -> arg
        | _                -> invalidOpf "Argument 1 is not compatible with type %A" typeof<'TArg2>
      RestMethodInvocation<'TItem, 'TArg1, 'TArg2, 'TResult> (b, newArg1, newArg2) :> _

module GenericParser =

  let private thruthy =
    [ "true"
      "1"
      "on" ]
    |> Seq.map CaseInsensitive
    |> Set.ofSeq

  let private customParsers = ConcurrentDictionary<Type, (string -> obj) voption> ()

  let private tryCreateParser (targetType : Type) =
    match targetType.GetMethod ("Parse", BindingFlags.Public ||| BindingFlags.Static) with
    | null -> ValueNone
    | m    ->
    match m.ReturnType = targetType with
    | false -> ValueNone
    | _ ->
    match m.GetParameters () with
    | [| p |] when p.ParameterType = typeof<string> ->
      ValueSome <| fun (raw: string) -> m.Invoke (null, [| raw |])
    | [| p1; p2 |] when p1.ParameterType = typeof<string> && p2.ParameterType = typeof<IFormatProvider> ->
      ValueSome <| fun (raw: string) -> m.Invoke (null, [| raw; CultureInfo.InvariantCulture |])
    | _ -> ValueNone

  let private tryCreateParserCached (targetType : Type) =
    let mutable res = Unchecked.defaultof<_>
    if not (customParsers.TryGetValue (targetType, &res)) then
      res <- tryCreateParser targetType
      customParsers.TryAdd (targetType, res) |> ignore
    res

  [<CompiledName("Parse")>]
  let parseObj (targetType : Type) (raw : string) =
    match targetType with
    | _ when targetType = typeof<string> ->
      raw :> obj
    | _ when targetType = typeof<bool> ->
      Set.contains (CaseInsensitive raw) thruthy |> box
    | _ when targetType = typeof<int8> ->
      SByte.Parse (raw, NumberStyles.Integer, CultureInfo.InvariantCulture) |> box
    | _ when targetType = typeof<int16> ->
      Int16.Parse (raw, NumberStyles.Integer, CultureInfo.InvariantCulture) |> box
    | _ when targetType = typeof<int32> ->
      Int32.Parse (raw, NumberStyles.Integer, CultureInfo.InvariantCulture) |> box
    | _ when targetType = typeof<int64> ->
      Int64.Parse (raw, NumberStyles.Integer, CultureInfo.InvariantCulture) |> box
    | _ when targetType = typeof<uint8> ->
      Byte.Parse (raw, NumberStyles.Integer, CultureInfo.InvariantCulture) |> box
    | _ when targetType = typeof<uint16> ->
      UInt16.Parse (raw, NumberStyles.Integer, CultureInfo.InvariantCulture) |> box
    | _ when targetType = typeof<uint32> ->
      UInt32.Parse (raw, NumberStyles.Integer, CultureInfo.InvariantCulture) |> box
    | _ when targetType = typeof<uint64> ->
      UInt64.Parse (raw, NumberStyles.Integer, CultureInfo.InvariantCulture) |> box
    | _ when targetType = typeof<char> ->
      match raw with
      | null                -> invalidOp "Unable to convert null to char."
      | s when s.Length = 1 -> box s.[0]
      | _                   -> invalidOpf "Unable to convert \"%s\" to char." raw
    | _ when targetType = typeof<DateTime> ->
      DateTime.Parse (raw, CultureInfo.InvariantCulture) |> box
    | _ when targetType = typeof<decimal> ->
      Decimal.Parse (raw, NumberStyles.Float, CultureInfo.InvariantCulture) |> box
    | _ when targetType = typeof<single> ->
      Single.Parse (raw, NumberStyles.Float, CultureInfo.InvariantCulture) |> box
    | _ when targetType = typeof<float> ->
      Double.Parse (raw, NumberStyles.Float, CultureInfo.InvariantCulture) |> box
    | _ when targetType.IsEnum ->
      Enum.Parse (targetType, raw, true)
    | _ ->
      match tryCreateParserCached targetType with
      | ValueSome parser -> parser raw
      | _ -> invalidOpf "Nu suitable method found to convert string to %A" targetType

  [<CompiledName("Parse")>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<RequiresExplicitTypeArguments>]
  let parse<'a> raw = parseObj typeof<'a> raw :?> 'a

