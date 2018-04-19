namespace NCoreUtils.AspNetCore.Rest

open System
open System.Reflection

[<AutoOpen>]
module private DefaultDefaultOrderPropertyHelpers =

  let inline eq a b = StringComparer.OrdinalIgnoreCase.Equals (a, b)

  let inline name (propertyInfo : PropertyInfo) = propertyInfo.Name

  let inline eqname value propertyInfo = name propertyInfo |> eq value

  let inline (|HasNamedProperty|_|) name (properties : PropertyInfo[]) =
    properties |> Array.tryFind (eqname name)


/// Provides default implementation for retrieving default ordering property.
type DefaultDefaultOrderProperty () =
  abstract Select<'a> : unit -> OrderByProperty
  /// <summary>
  /// Default implementation for retrieving default ordering property. Probes properties in following order: Updated,
  /// Created, Id, first defined property.
  /// </summary>
  /// <typeparam name="a">Type of the object ot return default ordering property for.</typeparam>
  /// <returns>
  /// Tuple containing default ordering property and whether default ordering direction is descending.
  /// </returns>
  default __.Select<'a> () =
      match typeof<'a>.GetProperties(BindingFlags.Instance ||| BindingFlags.Public) with
      | HasNamedProperty "Updated" p -> { Property = p; IsDescending = true }
      | HasNamedProperty "Created" p -> { Property = p; IsDescending = true }
      | HasNamedProperty "Id"      p -> { Property = p; IsDescending = false }
      | properties                   -> { Property = properties |> Array.head; IsDescending = false }

  interface IDefaultOrderProperty with
    member this.Select<'a> () = this.Select<'a> ()

  /// Gets shared instance of the default implementation for retrieving default ordering property.
  static member val SharedInstance = DefaultDefaultOrderProperty ()