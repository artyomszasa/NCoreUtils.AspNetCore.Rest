namespace NCoreUtils.AspNetCore.Rest

open System
open System.Reflection
open System.Runtime.CompilerServices

[<AutoOpen>]
module private DefaultDefaultOrderPropertyHelpers =

  let inline eq a b = StringComparer.OrdinalIgnoreCase.Equals (a, b)

  let inline name (propertyInfo : PropertyInfo) = propertyInfo.Name

  let inline eqname value propertyInfo = name propertyInfo |> eq value


/// Provides default implementation for retrieving default ordering property.
type DefaultDefaultOrderProperty () =

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member __.Select<'a> () =
      let properties = typeof<'a>.GetProperties(BindingFlags.Instance ||| BindingFlags.Public);
      match properties |> Array.tryFind (eqname "Updated") with
      | Some p -> struct (p, true)
      | _ ->
      match properties |> Array.tryFind (eqname "Created") with
      | Some p -> struct (p, true)
      | _ ->
      match properties |> Array.tryFind (eqname "Id") with
      | Some p -> struct (p, false)
      | _      -> struct (properties |> Array.head, false)


  interface IDefaultOrderProperty with
    member this.Select<'a> () = this.Select<'a> ()

  /// Gets shared instance of the default implementation for retrieving default ordering property.
  static member val SharedInstance = DefaultDefaultOrderProperty ()